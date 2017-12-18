using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockSortWithHeightMovement : MonoBehaviour {
	public string groundLevelName;
	public string firstLevelName;
	public string secondLevelName;
	public float tileSize;//we will use this as tile width & half of it as tileheight/size of other elements
	
	//tile values for different tile types
	public int blockTile;
	public int groundTile;
	public int invalidTile;
	
	//sprites for different tiles
	public Sprite tileSprite;
	public Sprite isoBlockSprite;//new isometric graphic

	//the user input keys
	int[,] groundFloorData;
	int[,] firstFloorData;
	int[,] secondFloorData;
	int rows;
	int cols;
	Vector2 middleOffset=new Vector2();//offset for aligning the level to middle of the screen
	Dictionary<GameObject,Vector2> groundOccupants;
	Dictionary<GameObject,Vector2> firstOccupants;
	Dictionary<GameObject,Vector2> secondOccupants;
	GameObject movingGO;
	Vector2 movingTileInitialPos=new Vector2(2,2);
	Vector2 movingTileCartPos=new Vector2();
	float speed=1;
	Vector3 movingDirection= new Vector3(0,0,1);
	float floorHeight;
	float tileZOffset=0;
	float tileYOffset=0;
	
	void Start () {
		floorHeight=tileSize/2.2f;
		groundOccupants=new Dictionary<GameObject, Vector2>();
		firstOccupants=new Dictionary<GameObject, Vector2>();
		secondOccupants=new Dictionary<GameObject, Vector2>();
		ParseLevel();//load text file & parse our level 2d array
		CreateLevel();//create the level based on the array
	}
	void ParseLevel(){
		ParseText(groundLevelName,out groundFloorData);
		ParseText(firstLevelName,out firstFloorData);
		ParseText(secondLevelName,out secondFloorData);
	}

    private void ParseText(string levelName,out int[,] floorData)
    {
        TextAsset textFile = Resources.Load (levelName) as TextAsset;
		string[] lines = textFile.text.Split (new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);//split by new line, return
		string[] nums = lines[0].Split(new[] { ',' });//split by ,
		rows=lines.Length;//number of rows
		cols=nums.Length;//number of columns
		floorData = new int[rows, cols];
        for (int i = 0; i < rows; i++) {
			string st = lines[i];
            nums = st.Split(new[] { ',' });
			for (int j = 0; j < cols; j++) {
                int val;
                if (int.TryParse (nums[j], out val)){
                	floorData[i,j] = val;
				}
                else{
                    floorData[i,j] = invalidTile;
				}
            }
        }
    }

    void CreateLevel(){
		//calculate the offset to align whole level to scene middle
		middleOffset.x=cols*tileSize*0.0625f+tileSize*0.125f;//this is changed for isometric
		middleOffset.y=rows*tileSize*0.25f+tileSize*0.25f;//this is changed for isometric
		middleOffset.y-=100;
		middleOffset.x-=50;
		
		AddFloor(groundFloorData,0);
	
		movingGO = new GameObject("moving");//create moving tile
		movingGO.transform.localScale=new Vector2(tileSize,tileSize);
		SpriteRenderer sr = movingGO.AddComponent<SpriteRenderer>();//add a sprite renderer
		sr.sprite=isoBlockSprite;//assign block sprite
		sr.sortingOrder=1;//this also need to have higher sorting order
		Color c= new Color(0.35f,0.35f,0.35f);
		sr.color=c;
		movingGO.transform.position=GetScreenPointFromLevelIndices((int)movingTileInitialPos.x,(int)movingTileInitialPos.y);//place in scene based on level indices
		movingTileCartPos=new Vector2(movingTileInitialPos.y*tileSize/2,movingTileInitialPos.x*tileSize/2);
		
		AddFloor(firstFloorData,1);
		AddFloor(secondFloorData,2);
		DepthSort();//sort depth after placing tiles, assign proper sorting order
	}

    private void AddFloor(int[,] floorData, int floorLevel)
    {
        float currentFloorHeight=floorHeight*floorLevel;
		int depth=(rows*cols)+((floorLevel-1)*(rows*cols))+1;
		GameObject tile;
		SpriteRenderer sr;
		Vector2 tmpPos;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
                int val=floorData[i,j];
				if(val!=invalidTile){//a valid tile
					if(val==blockTile){
						tile = new GameObject("block"+i.ToString()+"_"+j.ToString());//create new tile
						tile.transform.localScale=new Vector2(tileSize,tileSize);
						sr = tile.AddComponent<SpriteRenderer>();//add a sprite renderer
						sr.sprite=isoBlockSprite;//assign block sprite
						sr.sortingOrder=depth;
						tmpPos=GetScreenPointFromLevelIndices(i,j);
						tmpPos.y+=currentFloorHeight;
						tile.transform.position=tmpPos;
						switch(floorLevel){
							case 0:
								groundOccupants.Add(tile, new Vector2(i,j));
							break;
							case 1:
								firstOccupants.Add(tile, new Vector2(i,j));
							break;
							case 2:
								secondOccupants.Add(tile, new Vector2(i,j));
							break;
						}
						
						depth++;
					}	
				}	
            }
        }
    }

    void Update(){
		MoveBlock();
	}

    private void MoveBlock()
    {
        movingTileCartPos.x+=movingDirection.x*speed;
		tileZOffset+=movingDirection.z*speed/3;
		movingTileCartPos.y+=movingDirection.y*speed;
		Vector2 tempPt=CartesianToIsometric(new Vector2(movingTileCartPos.x,movingTileCartPos.y));
		tempPt.x-=middleOffset.x;
		tempPt.y*=-1;//unity y axis correction
		tempPt.y+=middleOffset.y;//we apply the offset outside the coordinate conversion to align the level in screen middle
		tempPt.y+=tileZOffset;
		movingGO.transform.position=tempPt;
		CheckAndSwitchDirection();
		DepthSort();
    }
	private void CheckAndSwitchDirection(){
		//check and switch height
		float newTileZOffset=tileZOffset;
		if(tileZOffset>2*floorHeight){
			newTileZOffset=2*floorHeight;
			movingDirection.z=-1;
		}else if(tileZOffset<0){
			newTileZOffset=0;
			movingDirection.z=1;
		}
		Vector2 tmpPos=movingGO.transform.position;
		if(newTileZOffset!=tileZOffset){
			tmpPos.y-=(tileZOffset-newTileZOffset);
			movingGO.transform.position=tmpPos;
			tileZOffset=newTileZOffset;
		}
		
		tmpPos.y-=tileZOffset;
		Vector2 movingTilePos=GetLevelIndicesFromScreenPoint(tmpPos);
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
		
		tmpPos=GetScreenPointFromLevelIndices(i,j);
		tmpPos.y+=tileZOffset;
		movingGO.transform.position=tmpPos;
		movingTileCartPos=new Vector2(j*tileSize/2,i*tileSize/2);
	}
	private void DepthSort()
    {
        float whichFloor=(tileZOffset/floorHeight);
		Vector2 tmpPos=movingGO.transform.position;
		tmpPos.y-=tileZOffset;
		Vector2 movingTilePos=GetLevelIndicesFromScreenPoint(tmpPos);
		//Debug.Log(movingTilePos.x.ToString()+","+movingTilePos.y.ToString());
		int blockColStart=(int)movingTilePos.y;
		int blockRowStart=(int)movingTilePos.x;
		int depth;
		int totalFloors=3;
		for(int floor=0;floor<totalFloors;floor++){
			depth=(floor*(rows*cols))+1;
			//sort rows before block
			for (int i = 0; i < blockRowStart; i++) {
				for (int j = 0; j < cols; j++) {
					depth=AssignDepth(i,j,depth,floor);
				}
			}
			//sort columns in same row before the block
			for (int i = blockRowStart; i < blockRowStart+2; i++) {
				for (int j = 0; j < blockColStart; j++) {
					depth=AssignDepth(i,j,depth,floor);
				}
			}
			//sort block
			float lower=Mathf.Floor(whichFloor);
			//int upper=Mathf.RoundToInt(whichFloor);
			if(lower==floor||floor==lower+1){
				for (int i = blockRowStart; i < blockRowStart+2; i++) {
					for (int j = blockColStart; j < blockColStart+2; j++) {
						if(movingTilePos.x==i&&movingTilePos.y==j){
							SpriteRenderer sr=movingGO.GetComponent<SpriteRenderer>();
							sr.sortingOrder=depth;//assign new depth
							depth++;//increment depth
						}else{
							depth=AssignDepth(i,j,depth,floor);
						}
					}
				}
			}else{
				for (int i = blockRowStart; i < blockRowStart+2; i++) {
					for (int j = blockColStart; j < blockColStart+2; j++) {
						depth=AssignDepth(i,j,depth,floor);
					}
				}
			}
			//sort columns in same row after the block
			for (int i = blockRowStart; i < blockRowStart+2; i++) {
				for (int j = blockColStart+2; j < cols; j++) {
					depth=AssignDepth(i,j,depth,floor);
				}
			}
			//sort rows after block
			for (int i = blockRowStart+2; i < rows; i++) {
				for (int j = 0; j < cols; j++) {
					depth=AssignDepth(i,j,depth,floor);
				}
			}
		}
    }

    private int AssignDepth(int i, int j, int depth,int floor)
    {
        SpriteRenderer sr;
		Vector2 pos=new Vector2();
		int val=0;
		switch(floor){
			case 0:
				val=groundFloorData[i,j];
			break;
			case 1:
				val=firstFloorData[i,j];
			break;
			case 2:
				val=secondFloorData[i,j];
			break;				
		}
		if(val!=groundTile){//a tile which needs depth sorting
			pos.x=i;
			pos.y=j;
			GameObject occuppant=GetOccupantAtPosition(pos,floor);//find the occuppant at this position
			if(occuppant!=null){
				sr=occuppant.GetComponent<SpriteRenderer>();
				sr.sortingOrder=depth;//assign new depth
				depth++;//increment depth
			}
		}
		return depth;
    }

    private GameObject GetOccupantAtPosition(Vector2 objPos,int floor)
    {//loop through the occupants to find the ball at given position
        GameObject ball;
		Dictionary<GameObject,Vector2> occupants= new Dictionary<GameObject, Vector2>();
		switch(floor){
			case 0:
				occupants=groundOccupants;
			break;
			case 1:
				occupants=firstOccupants;
			break;
			case 2:
				occupants=secondOccupants;
			break;				
		}
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
