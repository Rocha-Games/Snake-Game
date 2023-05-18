using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using System;

public class SnakeHead : MonoBehaviour {

    public enum MovementDirections{
        Up, 
        Down,
        Left,
        Right
    }

#region Public Fields
    
    public int PlayerNumber { get => _playerNumber; private set {} }

    public List<MovementDirections> ListPlayerMovements { get => _listPlayerMovements; private set{} }
#endregion


#region Private Serializable Fields
    [SerializeField] private GameObject _snakeBodyPrefab;
    [SerializeField] private Transform _snakeSprite;
    [SerializeField] private TextMeshProUGUI _textPlayerNum;
#endregion


#region Private Fields
    private MovementDirections _movementDirection;
    private List<GameObject> _listOfSnakeBodies = new();
    private Board _board;
    private UnityEvent<SnakeHead> _evtHitObstacle = new();
    private UnityEvent _evtAppleConsumed = new();
    private int _playerNumber;
    private float _lastMovementTime = 0;
    private float _turnDuration = 0;
    private bool _hasConsumedApple;
    private bool _hasBodyToSpawn = false;
    private PlayerInput _playerInput;
    private List<MovementDirections> _listPlayerMovements = new();
    private bool _isDead = false;

    #endregion


    #region MonoBehaviour CallBacks

    private void Awake() {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Start() {
        _turnDuration = GameManager.Instance.TurnDuration;
    }
    private void Update()
    {
        if(GameManager.Instance.GameState != GameManager.GameStates.Playing){
            return;
        }

        if(Time.time >= _lastMovementTime + _turnDuration){
            //even if no key is pressed, the snake will keep moving
            Move(_movementDirection);
            ListPlayerMovements.Add(_movementDirection);
            _lastMovementTime = Time.time;
        }
    }

#endregion


#region Private Methods
    private void RotateHead()
    {
        switch (_movementDirection)
        {
            case MovementDirections.Right:
                _snakeSprite.rotation = Quaternion.Euler(0, 0, 90);
                break;
            case MovementDirections.Up:
                _snakeSprite.rotation = Quaternion.Euler(0, 0, 180);
                break;
            case MovementDirections.Left:
                _snakeSprite.rotation = Quaternion.Euler(0, 0, 270);
                break;
            case MovementDirections.Down:
                _snakeSprite.rotation = Quaternion.Euler(0, 0, 0);
                break;
        }
    }

    private void Die()
    {
        _isDead = true;
        //Destroy all my body parts
        foreach (var body in _listOfSnakeBodies)
        {
            //Clean the board where the body was
            _board.SetTileContent(body.transform.position, GridTile.TileContents.Empty);
            Destroy(body.gameObject);
        }
        _listOfSnakeBodies.Clear();
        
        //Clean the board where the head was
        _board.SetTileContent(transform.position, GridTile.TileContents.Empty);

        //Hide head
        _snakeSprite.gameObject.SetActive(false); //we don't destroy the snake because it may be used in the replay

        _textPlayerNum.gameObject.SetActive(false);
        
        //Broadcast that I hit an obstacle and died
        _evtHitObstacle.Invoke(this);
    }
#endregion


#region Public Methods

    public void Initialize(int playerNumber_, Vector3 spawnPosition_, Board board_, UnityAction<SnakeHead> obstacleHitCallback_, MovementDirections firstMovementDirection_, UnityAction onAppleConsumedCallback_)
    {
        _playerNumber = playerNumber_;
        _textPlayerNum.text = GameManager.Instance.NumberOfPlayers > 1 ? $"P{_playerNumber}" : _textPlayerNum.text = "";

        _movementDirection = firstMovementDirection_;
        RotateHead();

        transform.position = spawnPosition_;
        _board = board_;

        _evtHitObstacle.AddListener(obstacleHitCallback_);
        _evtAppleConsumed.AddListener(onAppleConsumedCallback_);
    }

    public void AddBody(Vector3 spawnPosition_){
        var body = Instantiate(_snakeBodyPrefab, spawnPosition_, Quaternion.identity, _board.transform);
        _listOfSnakeBodies.Add(body);
        body.name = $"Snake Body {_listOfSnakeBodies.Count.ToString()}";

        _board.SetTileContent(spawnPosition_, GridTile.TileContents.Snake);
    }

    public void Move(MovementDirections direction_){

        if(_isDead){
            return;
        }
        
        var currentTileContent = _board.GetTileAtPosition(transform.position).Content;
        Vector3 nextHeadPosition = new();
        GridTile.TileContents nextTileContent = GridTile.TileContents.Empty;
        _movementDirection = direction_; //we do this for the replay

        //Calculate the next head position
        switch(direction_){
            case MovementDirections.Right:
                nextHeadPosition = transform.position + Vector3.right;
            break;
            case MovementDirections.Up:
                nextHeadPosition = transform.position + Vector3.up;
            break;
            case MovementDirections.Left:
                nextHeadPosition = transform.position + Vector3.left;
            break;
            case MovementDirections.Down:
                nextHeadPosition = transform.position + Vector3.down;
            break;
        }

        //Check the contents of the tile we are moving to
        nextTileContent = _board.GetTileAtPosition(nextHeadPosition).Content;
        if(nextTileContent == GridTile.TileContents.Snake || nextTileContent == GridTile.TileContents.Border)
        {
            //If I hit a Snake or a Border, I die
            Die();
        }
        else
        {

            if(nextTileContent == GridTile.TileContents.Apple){
                _hasConsumedApple = true;
                _hasBodyToSpawn = true;
            }

            //Move the Head
            var oldHeadPosition = transform.position;
            transform.position = nextHeadPosition;
            _board.SetTileContent(transform.position, GridTile.TileContents.Snake);

            //Move the Bodies
            Vector3 lastBodyPartPosition = new();
            for(int b = 0; b < _listOfSnakeBodies.Count; b++){                                                
                var bodyPart = _listOfSnakeBodies[b];

                if(b == 0){
                    //this is the first body part, so let's move it to where the head was
                    lastBodyPartPosition = bodyPart.transform.position;
                    bodyPart.transform.position = oldHeadPosition;

                    //maybe we are also the only body part?
                    if(_listOfSnakeBodies.Count == 1){
                        //Indeed! So we need to check if there's a body part to spawn                        
                        if(_hasBodyToSpawn){ //let's spawn a new piece of us!
                            AddBody(lastBodyPartPosition);
                            _hasBodyToSpawn = false;
                            break; //exit from the for loop, the last body part we added doesn't need to be moved anyways
                        }else{
                            //no new body part to spawn, but we still need to clean up the last tile we were at
                            _board.SetTileContent(lastBodyPartPosition, GridTile.TileContents.Empty);
                        }
                    }
                }
                else if (b == _listOfSnakeBodies.Count - 1){
                    //this is the last body part, so we need to set its old Tile position to Empty
                    var nextBodyPosition = lastBodyPartPosition;
                    lastBodyPartPosition = bodyPart.transform.position;
                    bodyPart.transform.position = nextBodyPosition;
                    
                    if(_hasBodyToSpawn){ //let's spawn a new piece of us!
                        AddBody(lastBodyPartPosition);
                        _hasBodyToSpawn = false;
                        break; //exit from the for loop, the last body part we added doesn't need to be moved anyways
                    }else{
                        _board.SetTileContent(lastBodyPartPosition, GridTile.TileContents.Empty);
                    }

                }else{
                    var nextBodyPosition = lastBodyPartPosition;
                    lastBodyPartPosition = bodyPart.transform.position; //save position for the next body part
                    
                    bodyPart.transform.position = nextBodyPosition; //move
                }
            }
            //after all body parts were moved, let's tell the game we ate an apple, so it can be spawned
            if(_hasConsumedApple){
                _hasConsumedApple = false;
                _evtAppleConsumed.Invoke();
            }
        }
        RotateHead();
    }

    public void ReadInput(InputAction.CallbackContext context_){
        
        Vector2 movement = context_.ReadValue<Vector2>();

        if(movement.x > 0 && _movementDirection != MovementDirections.Left){
            _movementDirection = MovementDirections.Right;
        }else if(movement.x < 0 && _movementDirection != MovementDirections.Right){
            _movementDirection = MovementDirections.Left;
        }else if(movement.y > 0 && _movementDirection != MovementDirections.Down){
            _movementDirection = MovementDirections.Up;
        }else if(movement.y < 0 && _movementDirection != MovementDirections.Up){
            _movementDirection = MovementDirections.Down;
        }
    }

    public void Respawn(Vector3 position_, int snakeLength_)
    {
        // Debug.Log($"{name} respawning at {position_}!");
        transform.position = position_;
        _snakeSprite.gameObject.SetActive(true);
        _board.SetTileContent(transform.position, GridTile.TileContents.Snake);

        //now spawn the snake body
        var isLeftHalfOfBoard = position_.x <= _board.Width / 2;

        List<Vector3> listOfSpawnPoints = new();
        if(isLeftHalfOfBoard){
            for(int i = 1; i < snakeLength_; i++){
                listOfSpawnPoints.Add(position_ + (Vector3.left * i));
            }
            _movementDirection = MovementDirections.Right;
        }else{
            for(int i = 1; i < snakeLength_; i++){
                listOfSpawnPoints.Add(position_ + (Vector3.right * i));
            }
            _movementDirection = MovementDirections.Left;
        }
        RotateHead();

        Vector3 bodySpawnPosition = new();
        for(int j = 0; j < snakeLength_ - 1; j++){
            // Debug.Log($"Spawning a body at {listOfSpawnPoints[j]}");
            bodySpawnPosition = listOfSpawnPoints[j];
            AddBody(bodySpawnPosition);
            _board.SetTileContent(bodySpawnPosition, GridTile.TileContents.Snake);
        }

        _textPlayerNum.gameObject.SetActive(true);
        _isDead = false;
    }
    #endregion
}
