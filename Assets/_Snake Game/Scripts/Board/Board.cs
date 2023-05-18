using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour{

#region Public Fields
    public int Width { get => _columns; private set {} }
    public int Height { get => _rows; private set {} }
    public Vector2 TileSize { get => _tileSize; private set {} }
    public List<Vector3> ListApplePositions { get => _listApplePositions; private set{} }
    #endregion


    #region Private Serializable Fields
    [SerializeField] private int _columns = 15;
    [SerializeField] private int _rows = 10;
    [SerializeField] private GameObject _playAreaTilePrefab;
    [SerializeField] private GameObject _borderTilePrefab;
    [SerializeField] private GameObject _appleTilePrefab;
#endregion


#region Private Fields
    private List<List<GridTile>> _board = new();
    private Vector2 _tileSize = new();
    private GameObject _apple;
    private UnityEvent _evtBoardFilled = new();
    private List<Vector3> _listApplePositions = new();
    #endregion


    #region MonoBehaviour CallBacks

    #endregion


    #region Private Methods

    #endregion


    #region Public Methods
    public void CreateBoard(){
        _tileSize = _playAreaTilePrefab.GetComponent<SpriteRenderer>().bounds.size;
        
        Vector2 tilePosition = new();

		for(int y = 0; y < _rows; y++){
            
			//create a new grid line
			List<GridTile> gridLine = new();

			//Fill the line with empty tiles
			for(int x = 0; x < _columns; x++){
				
                //Create a new tile
                GameObject tileObj;
                GridTile.TileContents tileContent = GridTile.TileContents.Empty;
                if(y == 0 || x == 0 || y == _rows - 1 || x == _columns - 1){
                    tileObj = Instantiate(_borderTilePrefab, transform);
                    tileContent = GridTile.TileContents.Border;
                }else{
                    tileObj = Instantiate(_playAreaTilePrefab, transform);
                }
                var gridTile = tileObj.GetComponent<GridTile>();
				
                //Set the new tile's Board Position and Content
                tilePosition.x = x;
                tilePosition.y = y;
                gridTile.Initialize(tileContent, (y * _columns) + x, tilePosition);
                
                tileObj.transform.position = tilePosition;

				gridLine.Add(gridTile);
			}
			
			//add the grid line to the list of grid tiles. Bottom to Top, Left to Right
			_board.Add(gridLine);
		}
    }
    public GridTile GetTileAtPosition(Vector3 position_){
        int row = (int)position_.y;
        int column = (int)position_.x;
        return _board[row][column];
    }
    
    public void SetTileContent(Vector3 position_, GridTile.TileContents content_){
        int row = (int)position_.y;
        int column = (int)position_.x;

        _board[row][column].SetContent(content_);
    }

    public IEnumerator SpawnAppleCor(){
        bool emptyTileFound = false;
        Vector3 spawnPosition = new();
        while(!emptyTileFound){
            //get random column and row values
            var boardColumn = Random.Range(1, Width - 1);
            var boardRow = Random.Range(1, Height - 1);
            spawnPosition.x = boardColumn;
            spawnPosition.y = boardRow;

            var tileContent = GetTileAtPosition(spawnPosition).Content;
            if(tileContent == GridTile.TileContents.Empty){
                emptyTileFound = true;
            }
            yield return null;
        }

        _apple = Instantiate(_appleTilePrefab, spawnPosition, Quaternion.identity, transform);
        SetTileContent(spawnPosition, GridTile.TileContents.Apple);
        _listApplePositions.Add(spawnPosition);
    }

    public IEnumerator MoveAppleCor(){
        bool emptyTileFound = false;
        Vector3 newPosition = new();
        while(!emptyTileFound){
            //get random column and row values
            var boardColumn = Random.Range(1, Width - 1);
            var boardRow = Random.Range(1, Height - 1);
            newPosition.x = boardColumn;
            newPosition.y = boardRow;

            var tileContent = GetTileAtPosition(newPosition).Content;
            if(tileContent == GridTile.TileContents.Empty){
                emptyTileFound = true;
            }
            yield return null;
        }

        //we don't need to set the old position as Empty because now it's certainly a Snake Head
        _apple.transform.position = newPosition;
        _listApplePositions.Add(newPosition);
        SetTileContent(newPosition, GridTile.TileContents.Apple);
    }

    public void SetApplePosition(Vector3 position_, bool setPreviousPosEmpty_){
        if(setPreviousPosEmpty_){
            SetTileContent(_apple.transform.position, GridTile.TileContents.Empty);
        }

        _apple.transform.position = position_;
        SetTileContent(position_, GridTile.TileContents.Apple);
    }

    //Every time an apple is consumed we check if the board is filled with only snakes
    public void CheckBoardFilledWithSnakes(UnityAction onBoardFilledCallback_){
        _evtBoardFilled.AddListener(onBoardFilledCallback_);
        Vector3 position = new();
        bool isAllSnake = true;

        for (int i = 0; i < _rows; i++)
        {
            for (int j = 0; j < _columns; j++)
            {
                position.x = j;
                position.y = i;
                var tileContent = GetTileAtPosition(position).Content;
                if(tileContent == GridTile.TileContents.Empty || tileContent == GridTile.TileContents.Apple){
                    isAllSnake = false;
                }
            }
        }

        if(isAllSnake){
            _evtBoardFilled.Invoke();
        }

        _evtBoardFilled.RemoveAllListeners();
    }
    #endregion
}