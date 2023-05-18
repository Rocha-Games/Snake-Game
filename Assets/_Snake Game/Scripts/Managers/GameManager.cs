using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour 
{
    public static GameManager Instance;

    public enum GameStates{
        Initializing,
        CountingDown,
        Playing,
        Paused,
        ShowGameOver,
        GameOver,
        PrepareReplay,
        Replaying
    }

#region Public Fields
    public GameStates GameState { get => _gameState; private set{} }
    public Board Board { get => _board; private set{} }

    public float TurnDuration { get => _turnDuration; private set{} }

    public int NumberOfPlayers { get => _numberOfPlayers; private set{} }
    #endregion


    #region Private Serializable Fields
    [Range(1,4)][SerializeField] private int _numberOfPlayers = 1;
    [SerializeField] private float _countDownDuration = 3;
    [SerializeField] private int _snakeInitialLength = 3;

    [Tooltip("Snakes will move only once every turn")]
    [SerializeField] private float _turnDuration = 1;

    [SerializeField] private float _maxCameraOrthoSize = 10f;
    [Header("Board Size is defined in the Board Prefab")]
    [SerializeField] private GameObject _boardPrefab;
    [SerializeField] private GameObject _snakePrefab;
#endregion


#region Private Fields
    private GameStates _gameState = GameStates.Initializing;
    private Board _board;
    private bool _countDownFinished = false;
    private bool _isGameOver = false;
    private List<SnakeHead> _listOfPlayers = new();
    private int _winnerPlayerNum = 0;
    private Dictionary<SnakeHead, List<SnakeHead.MovementDirections>> _dicPlayerMovements = new();
    private Dictionary<SnakeHead, Vector3> _dicPlayersSpawnPoints = new();
    private List<Vector3> _listApplePositions = new();
    private float _lastTurnTime = 0;
    private float _totalTurns = 0;
    private float _numSnakesAlive = 0;
#endregion


    #region MonoBehaviour CallBacks
    void Awake(){
        if(Instance == null){
            Instance = this;
            // DontDestroyOnLoad(Instance);
        }else{
            Destroy(gameObject);
            return;
        }
    }

    void Start(){

    }

    void Update(){
        
        //Super simple State Machine
        switch (_gameState)
        {
            case GameStates.Initializing:

                //Instantiate and Create the Board
                _board = Instantiate(_boardPrefab).GetComponent<Board>();
                _board.CreateBoard();
            
                //Setup the Camera based on board size
                SetupCameraOrthoAndSize();

                //Spawn Players
                StartCoroutine(SpawnPlayersCor());

                GUIManager.Instance.ShowCountdown(_countDownDuration, OnCountdownFinished);

                _gameState = GameStates.CountingDown;
                break;
                
            case GameStates.CountingDown:
                if(_countDownFinished){
                    StartCoroutine(_board.SpawnAppleCor());
                    _gameState = GameStates.Playing;
                }
                break;
                
            case GameStates.Playing:
                if(_isGameOver){
                    _gameState = GameStates.ShowGameOver;
                }

                //count total turns
                if(Time.time > _lastTurnTime + _turnDuration){
                    _totalTurns++;
                    _lastTurnTime = Time.time;
                }

                break;
                
            case GameStates.Paused:
            
                break;

            case GameStates.ShowGameOver:

                if(NumberOfPlayers > 1){
                    GUIManager.Instance.ShowPlayerWon(_winnerPlayerNum);
                }else{
                    GUIManager.Instance.ShowGameOver();
                }

                _gameState = GameStates.GameOver;
                break;
                
            case GameStates.GameOver:
                break;

            case GameStates.PrepareReplay:
                //we gonna use a dictionary/map to collect all player's movements that were stored by themselves
                for(int i = 0; i < _numberOfPlayers; i++){
                    _dicPlayerMovements.Add(_listOfPlayers[i], _listOfPlayers[i].ListPlayerMovements);
                }

                _listApplePositions = _board.ListApplePositions;
                StartCoroutine(ReplayLastGameCor());
                _gameState = GameStates.Replaying;
            break;

            case GameStates.Replaying:
                //
            break;

            default:
                break;
        }       
    }
    #endregion


    #region Private Methods
    private void SetupCameraOrthoAndSize()
    {
        //Ortho size is HALF of the amount of Unity units displayed vertically
        //So if Ortho is 5, we can see 10 tiles, from -5 to +5
        Camera.main.orthographicSize = Mathf.Clamp(Board.Height * 0.6f, 0, _maxCameraOrthoSize);
        Camera.main.transform.position = new Vector3(Board.TileSize.x * (Board.Width - 1) / 2f, Board.TileSize.y * (Board.Height - 1) / 2f, Camera.main.transform.position.z );
    }

    

    private IEnumerator SpawnPlayersCor(){

        //Randomly spawn players around the board
        //Many checks are performed to make sure a player don't spawn out of bound, or on top of another player
        for(int i = 0; i < NumberOfPlayers; i++){
            //when finding a spawn point, we take in consideration the initial length of the snake.            
            //if the spawn position is at the 2nd or 3rd quadrant of the board, we spawn the snake facing right
            //if the spawn position is at the 1st or 4th quadrant of the board, we spawn the snake facing left
            //when the snake is spawned, its head will occupy the initial spawn position, and the body will be created in the opposite direction that the head is facing
            //Considering an initial length of 3, we spawn 1 head (h), and 2 bodies (b)
                        
            //////////////////////////////////////////////////
            //                      |                       //
            //        bbh ->        |                       //
            //                      |                       //
            //                      |                       //
            //                  2nd | 1st        <- hbb     //
            //----------------------------------------------//
            //                  3rd | 4th                   //
            //                      |                       //
            //                      |                       //
            //                      | <- hbb                //
            //               bbh -> |                       //
            //////////////////////////////////////////////////

            // Moreover row and column 0 are forbidden, they are the bounds of the board
            // The last row and column are also forbidden as they are bounds as well

            List<Vector3> listOfSpawnPoints = new();

            //Make sure the Spawn Point and the tiles where the body parts are going to be spawned are Empty
            bool leftHalfOfBoard = true;
            bool emptyTilesFound = false;
            while(!emptyTilesFound){
                emptyTilesFound = true; //starts as true because if something goes wrong we just set to false and the while loop is executed again

                //get random column and row values
                var boardColumn = Random.Range(_snakeInitialLength, Board.Width - _snakeInitialLength - 1);
                var boardRow = Random.Range(1, Board.Height - 1);
                Vector3 spawnPosition = new Vector3(boardColumn, boardRow); //this is the head spawn point position
                var headTile = Board.GetTileAtPosition(spawnPosition);
                if(headTile.Content != GridTile.TileContents.Empty){
                    //not good, tile is not Empty, try again
                    emptyTilesFound = false;
                    continue; //continue skips back to the start of the while loop
                }

                //head is ok, add to the list of spawn points to be used later
                listOfSpawnPoints.Add(spawnPosition);

                //now check the body parts
                leftHalfOfBoard = spawnPosition.x <= Board.Width / 2f;
                for(int j = 1; j < _snakeInitialLength; j++){
                    if(leftHalfOfBoard){
                        //2nd or 3rd quadrants, spawn bodies to the left of the head, snake will face right
                        spawnPosition = spawnPosition - Vector3.right;
                    }else{
                        //1st or 4th quadrants, spawn bodies to the right of the head, snake will face left
                        spawnPosition = spawnPosition + Vector3.right;
                    }
                    var bodyTile = Board.GetTileAtPosition(spawnPosition);
                    if(bodyTile.Content != GridTile.TileContents.Empty){
                        //not good, clean things up and try again
                        listOfSpawnPoints.Clear();
                        emptyTilesFound = false;
                        continue; //continue skips back to the start of the while loop
                    }
                    listOfSpawnPoints.Add(spawnPosition);
                }
                //if we got this far, we found all empty tiles! Let's go!
                yield return null;
            }

            //spawn player (snake's head)
            var headSpawnPosition = listOfSpawnPoints[0]; //0 is the Head, no doubts
            int playerNumber = i + 1;
            var controlScheme = $"Keyboard {playerNumber}";

            //Note that we are using PlayerInput.Instantiate and forcing the Input to be the same Keyboard for all players
            //If in the future we need to change the input to also accept gamepads, this must be changed.
            //I did this because, as stated even in Unity Forums, the new Input System doesn't allow different players using the same Input, which
            //makes sense for gamepads, but on the good old days people played on the same keyboard. Unity devs are probably younger than me XD
            var snakeObj = PlayerInput.Instantiate(_snakePrefab, controlScheme: controlScheme, pairWithDevice: Keyboard.current);
            
            // var snakeObj = PlayerInput.Instantiate(_snakePrefab, controlScheme: controlScheme, pairWithDevice: Gamepad.current);
            // var snakeObj = Instantiate(_snakePrefab, headSpawnPosition, Quaternion.identity, Board.transform);

            snakeObj.transform.position = headSpawnPosition;
            snakeObj.transform.parent = Board.transform;
            var snake = snakeObj.GetComponent<SnakeHead>();
            snake.Initialize(playerNumber, headSpawnPosition, _board, OnPlayerHitObstacle, leftHalfOfBoard ? SnakeHead.MovementDirections.Right : SnakeHead.MovementDirections.Left, OnAppleConsumed);
            Board.SetTileContent(headSpawnPosition, GridTile.TileContents.Snake);
            _listOfPlayers.Add(snake);
            _numSnakesAlive++;
            _dicPlayersSpawnPoints.Add(snake, headSpawnPosition);
            
            //now spawn the snake body
            var isLeftHalfOfBoard = headSpawnPosition.x <= Board.Width / 2;
            Vector3 bodySpawnPosition = new();
            for(int j = 1; j < _snakeInitialLength; j++){
                bodySpawnPosition = listOfSpawnPoints[j];
                snake.AddBody(bodySpawnPosition);
                Board.SetTileContent(bodySpawnPosition, GridTile.TileContents.Snake);
            }
        }
    }

    private void OnPlayerHitObstacle(SnakeHead snake_)
    {
        _winnerPlayerNum = snake_.PlayerNumber;
        _numSnakesAlive--;
        if(_numSnakesAlive <= 0){
            _isGameOver = true;
        }
    }

    private void OnAppleConsumed()
    {
        if(_gameState == GameStates.Replaying){
            if(_listApplePositions.Count > 0){
                _board.SetApplePosition(_listApplePositions[0], false);
                _listApplePositions.RemoveAt(0);
            }
            return;
        }

        _board.CheckBoardFilledWithSnakes(OnBoardFilledWithSnakes);
        if(!_isGameOver){
            StartCoroutine(_board.MoveAppleCor());
        }
    }

    private void OnBoardFilledWithSnakes()
    {
        _isGameOver = true;
    }

    private void OnCountdownFinished()
    {
        _countDownFinished = true;
    }

    private IEnumerator ReplayLastGameCor(){

        int turn = 0;
        //Show first apple
        _board.SetApplePosition(_listApplePositions[0], true);
        _listApplePositions.RemoveAt(0);

        //"Spawn" players
        foreach(var pair in _dicPlayersSpawnPoints){
            pair.Key.Respawn(pair.Value, _snakeInitialLength);
        }

        //play all turns
        // Debug.Log($"Total Turns: {_totalTurns}");
        while(turn < _totalTurns){
            yield return new WaitForSeconds(_turnDuration);

            // Debug.Log($"Playing turn {turn}");
            foreach(var player in _dicPlayerMovements){
                //if player was still alive that turn
                if(turn < player.Value.Count){
                    var movement = player.Value[turn]; //value is List<SnakeHead.MovementDirections>
                    player.Key.Move(movement);
                }
            }
            turn++;
        }
        _gameState = GameStates.ShowGameOver;
    }
    #endregion

#region Public Methods

    public void RestartGame(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void ShowReplay(){
        _gameState = GameStates.PrepareReplay;
    }
#endregion
}