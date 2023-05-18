using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class GUIManager : MonoBehaviour {

    public static GUIManager Instance;

#region Public Fields

#endregion


#region Private Serializable Fields
    [SerializeField] private GameObject _gameOverContent;
    [SerializeField] private GameObject _countdownContent;
    [SerializeField] private GameObject _playerWonContent;
    [SerializeField] private TextMeshProUGUI _textCountDownValue, _textPlayerWon;
    [SerializeField] private Button _btnPlayAgain, _btnReplay;
#endregion


#region Private Fields
    private UnityEvent evtCountDownFinished = new();
#endregion


#region MonoBehaviour CallBacks
    void Awake(){
        if(Instance == null){
            Instance = this;
        }else{
            Destroy(gameObject);
            return;
        }
    }

    void Start(){
        _gameOverContent.SetActive(false);
        _countdownContent.SetActive(false);
        _textCountDownValue.text = "";
        
        _btnPlayAgain.gameObject.SetActive(false);
        _btnReplay.gameObject.SetActive(false);

        _btnPlayAgain.onClick.AddListener(OnPlayAgainPressed);
        _btnReplay.onClick.AddListener(OnReplayPressed);
    }
#endregion


#region Private Methods
    IEnumerator ShowCountdownCor(float countdownDuration_){
        // Debug.Log($"Starting countdown: {countdownDuration_}s");
        float countdownDuration = countdownDuration_;
        _countdownContent.SetActive(true);
        _textCountDownValue.text = countdownDuration.ToString();

        while(countdownDuration > 0){
            countdownDuration -= Time.deltaTime;            
            _textCountDownValue.text = Mathf.CeilToInt(countdownDuration).ToString();
            yield return null;
        }
        _textCountDownValue.text = "GO!";
        yield return new WaitForSeconds(1f);
        _countdownContent.SetActive(false);
        evtCountDownFinished.Invoke();
    }

    private void OnPlayAgainPressed(){
        _btnPlayAgain.interactable = false;
        GameManager.Instance.RestartGame();
    }

    private void OnReplayPressed(){
        _btnReplay.interactable = false;
        _btnReplay.gameObject.SetActive(false);
        _btnPlayAgain.gameObject.SetActive(false);
        _gameOverContent.SetActive(false);
        _playerWonContent.SetActive(false);

        GameManager.Instance.ShowReplay();
    }
#endregion


#region Public Methods
    public void ShowCountdown(float countdownDuration_, UnityAction callback_){
        evtCountDownFinished.AddListener(callback_);
        StartCoroutine(ShowCountdownCor(countdownDuration_));
    }

    internal void ShowGameOver(){
        _btnPlayAgain.gameObject.SetActive(true);
        _btnReplay.gameObject.SetActive(true);
        _gameOverContent.SetActive(true);
    }

    internal void ShowPlayerWon(int winnerPlayerNum_){
        _textPlayerWon.text = $"Player {winnerPlayerNum_} Wins!";
        _btnPlayAgain.gameObject.SetActive(true);
        _btnReplay.gameObject.SetActive(true);
        _playerWonContent.SetActive(true);
    }
    #endregion
}