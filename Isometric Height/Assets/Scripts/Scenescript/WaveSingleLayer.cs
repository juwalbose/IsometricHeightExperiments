using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaveSingleLayer : MonoBehaviour {
	public string levelName;//name of the text file in resources folder
	public float tileSize;//we will use this as tile width & half of it as tileheight/size of other elements
	
	//tile values for different tile types
	public int blockTile;
	public int groundTile;
	public int invalidTile;
	
	//sprites for different tiles
	public Sprite tileSprite;
	public Sprite isoBlockSprite;//new isometric graphic

	//the user input keys
	int[,] levelData;//level array
	int rows;
	int cols;
	Vector2 middleOffset=new Vector2();//offset for aligning the level to middle of the screen
	Dictionary<GameObject,Vector2> occupants;//reference to balls & hero
	GameObject movingGO;
	Vector2 movingTileInitialPos=new Vector2(2,2);
	Vector2 movingTileCartPos=new Vector2();
	float speed=1;
	Vector2 movingDirection= new Vector2(1,0);
	float currentWaveValue=0;
	float elapsedTime=0;
	
	void Start () {
		occupants=new Dictionary<GameObject, Vector2>();
		ParseLevel();//load text file & parse our level 2d array
		CreateLevel();//create the level based on the array
	}
	void ParseLevel(){
		TextAsset textFile = Resources.Load (levelName) as TextAsset;
		string[] lines = textFile.text.Split (new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);//split by new line, return
		string[] nums = lines[0].Split(new[] { ',' });//split by ,
		rows=lines.Length;//number of rows
		cols=nums.Length;//number of columns
		levelData = new int[rows, cols];
        for (int i = 0; i < rows; i++) {
			string st = lines[i];
            nums = st.Split(new[] { ',' });
			for (int j = 0; j < cols; j++) {
                int val;
                if (int.TryParse (nums[j], out val)){
                	levelData[i,j] = val;
				}
                else{
                    levelData[i,j] = invalidTile;
				}
            }
        }
	}
	void CreateLevel(){//changed to depth swap with all tiles as ground tiles also need to participate
		//calculate the offset to align whole level to scene middle
		middleOffset.x=cols*tileSize*0.0625f+tileSize*0.125f;//this is changed for isometric
		middleOffset.y=rows*tileSize*0.25f+tileSize*0.25f;//this is changed for isometric
		GameObject tile;
		SpriteRenderer sr;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
                int val=levelData[i,j];
				if(val!=invalidTile){//a valid tile
					if(val==groundTile){
						tile = new GameObject("tile"+i.ToString()+"_"+j.ToString());//create new tile
						tile.transform.localScale=new Vector2(tileSize,tileSize);//size is critical for isometric shape
						sr = tile.AddComponent<SpriteRenderer>();//add a sprite renderer
						sr.sprite=tileSprite;//assign tile sprite
						sr.sortingOrder=1;
						tile.transform.position=GetScreenPointFromLevelIndices(i,j);//place in scene based on level indices
						occupants.Add(tile, new Vector2(i,j));
					}else if(val==blockTile){
						tile = new GameObject("block"+i.ToString()+"_"+j.ToString());//create new tile
						tile.transform.localScale=new Vector2(tileSize,tileSize);
						sr = tile.AddComponent<SpriteRenderer>();//add a sprite renderer
						sr.sprite=isoBlockSprite;//assign block sprite
						sr.sortingOrder=1;//this also need to have higher sorting order
						tile.transform.position=GetScreenPointFromLevelIndices(i,j);//place in scene based on level indices
						occupants.Add(tile, new Vector2(i,j));//store the level indices of block in dict
					}	
				}else if(i==movingTileInitialPos.x && j==movingTileInitialPos.y){
					movingGO = new GameObject("moving");//create new tile
					movingGO.transform.localScale=new Vector2(tileSize,tileSize);
					sr = movingGO.AddComponent<SpriteRenderer>();//add a sprite renderer
					sr.sprite=isoBlockSprite;//assign block sprite
					sr.sortingOrder=1;//this also need to have higher sorting order
					Color c= new Color(0.35f,0.35f,0.35f);
					sr.color=c;
					movingGO.transform.position=GetScreenPointFromLevelIndices(i,j);//place in scene based on level indices
					movingTileCartPos=new Vector2(j*tileSize/2,i*tileSize/2);
				}	
            }
        }
		DepthSort();//sort depth after placing tiles, assign proper sorting order
	}
    void Update(){
		elapsedTime+=Time.deltaTime;
		currentWaveValue=Mathf.Sin(4*elapsedTime)*40;
		MoveBlock();
	}

    private void MoveBlock()
    {
		movingTileCartPos.x+=movingDirection.x*speed;
		movingTileCartPos.y+=movingDirection.y*speed;
		Vector2 tempPt=CartesianToIsometric(new Vector2(movingTileCartPos.x,movingTileCartPos.y));
		tempPt.x-=middleOffset.x;
		tempPt.y*=-1;//unity y axis correction
		tempPt.y+=middleOffset.y;//we apply the offset outside the coordinate conversion to align the level in screen middle
		tempPt.y+=currentWaveValue;
		movingGO.transform.position=tempPt;
		CheckAndSwitchDirection();
		DepthSort();
    }
	private void CheckAndSwitchDirection(){
		Vector2 movingTilePos=movingGO.transform.position;
		movingTilePos.y-=currentWaveValue;
		movingTilePos=GetLevelIndicesFromScreenPoint(movingTilePos);
		int i=0,j=0;
		if(movingDirection.x==1 && movingTilePos.x==2 &&movingTilePos.y==4){
			movingDirection.x=0;
			movingDirection.y=1;
			i=2;
			j=4;
		}else if(movingDirection.y==1 && movingTilePos.x==4 &&movingTilePos.y==4){
			movingDirection.x=-1;
			movingDirection.y=0;
			i=4;
			j=4;
		}else if(movingDirection.x==-1 && movingTilePos.x==4 &&movingTilePos.y==1){
			movingDirection.x=0;
			movingDirection.y=-1;
			i=4;
			j=2;
		}else if(movingDirection.y==-1 && movingTilePos.x==1 &&movingTilePos.y==2){
			movingDirection.x=1;
			movingDirection.y=0;
			i=2;
			j=2;
		}else{
			return;
		}
		movingTilePos=GetScreenPointFromLevelIndices(i,j);
		movingTilePos.y+=currentWaveValue;
		movingGO.transform.position=movingTilePos;
		movingTileCartPos=new Vector2(j*tileSize/2,i*tileSize/2);
	}
	private void DepthSort()
    {
        Vector2 movingTilePos=movingGO.transform.position;
		movingTilePos.y-=currentWaveValue;
		movingTilePos=GetLevelIndicesFromScreenPoint(movingTilePos);
		//Debug.Log(movingTilePos.x.ToString()+","+movingTilePos.y.ToString());
		int blockColStart=(int)movingTilePos.y;
		int blockRowStart=(int)movingTilePos.x;
		int depth=1;
		
		//sort rows before block
		for (int i = 0; i < blockRowStart; i++) {
			for (int j = 0; j < cols; j++) {
				depth=AssignDepth(i,j,depth);
			}
		}
		//sort columns in same row before the block
		for (int i = blockRowStart; i < blockRowStart+2; i++) {
			for (int j = 0; j < blockColStart; j++) {
				depth=AssignDepth(i,j,depth);
			}
		}
		//sort block
		for (int i = blockRowStart; i < blockRowStart+2; i++) {
			for (int j = blockColStart; j < blockColStart+2; j++) {
				if(movingTilePos.x==i&&movingTilePos.y==j){
					SpriteRenderer sr=movingGO.GetComponent<SpriteRenderer>();
					sr.sortingOrder=depth;//assign new depth
					depth++;//increment depth
				}else{
					depth=AssignDepth(i,j,depth);
				}
			}
		}
		//sort columns in same row after the block
		for (int i = blockRowStart; i < blockRowStart+2; i++) {
			for (int j = blockColStart+2; j < cols; j++) {
				depth=AssignDepth(i,j,depth);
			}
		}
		//sort rows after block
		for (int i = blockRowStart+2; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
				depth=AssignDepth(i,j,depth);
			}
		}
    }

    private int AssignDepth(int i, int j, int depth)
    {
        SpriteRenderer sr;
		Vector2 pos=new Vector2();
		pos.x=i;
		pos.y=j;
		GameObject occuppant=GetOccupantAtPosition(pos);//find the occuppant at this position
		if(occuppant!=null){
			sr=occuppant.GetComponent<SpriteRenderer>();
			sr.sortingOrder=depth;//assign new depth
			depth++;//increment depth
		}
		return depth;
    }

    private GameObject GetOccupantAtPosition(Vector2 objPos)
    {//loop through the occupants to find the ball at given position
        GameObject ball;
		foreach (KeyValuePair<GameObject, Vector2> pair in occupants)
		{
			if (pair.Value == objPos)
			{
				ball = pair.Key;
				return ball;
			}
		}
		return null;
    }

	Vector2 GetScreenPointFromLevelIndices(int row,int col){
		//converting indices to position values, col determines x & row determine y
		Vector2 tempPt=CartesianToIsometric(new Vector2(col*tileSize/2,row*tileSize/2));//removed the '-' inthe y part as axis correction can happen after coversion
		tempPt.x-=middleOffset.x;//we apply the offset outside the coordinate conversion to align the level in screen middle
		tempPt.y*=-1;//unity y axis correction
		tempPt.y+=middleOffset.y;//we apply the offset outside the coordinate conversion to align the level in screen middle
		return tempPt;
	}
	Vector2 CartesianToIsometric(Vector2 cartPt){
		Vector2 tempPt=new Vector2();
		tempPt.x=cartPt.x-cartPt.y;
		tempPt.y=(cartPt.x+cartPt.y)/2;
		return (tempPt);
	}
	//the reverse methods to find indices from a screen point
	Vector2 GetLevelIndicesFromScreenPoint(float xVal,float yVal){
		Vector2 tempPt=new Vector2(xVal,yVal);
		tempPt.y-=middleOffset.y;
		tempPt.y*=-1;
		tempPt.x+=middleOffset.x;
		tempPt=IsometricToCartesian(tempPt);
		return new Vector2((int)(tempPt.y/(tileSize/2)),(int)(tempPt.x/(tileSize/2)));
	}
	Vector2 GetLevelIndicesFromScreenPoint(Vector2 pos){
		return GetLevelIndicesFromScreenPoint(pos.x,pos.y);
	}
	//the reverse conversion method for isometric to cartesian coordinate conversion
	Vector2 IsometricToCartesian(Vector2 isoPt){
		Vector2 tempPt=new Vector2();
		tempPt.x=(2*isoPt.y+isoPt.x)/2;
		tempPt.y=(2*isoPt.y-isoPt.x)/2;
		return (tempPt);
	}
}
