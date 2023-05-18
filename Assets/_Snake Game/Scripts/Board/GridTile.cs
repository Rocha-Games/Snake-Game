using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GridTile : MonoBehaviour {

public enum TileContents{
        Empty = 0,
        Snake = 1,
        Apple = 2,
        Border = 3
    }

#region Public Fields
    [HideInInspector] public TileContents Content { get => _content; private set{} }
    [HideInInspector] public Vector3 TilePosition { get => _tilePosition; private set{} }
#endregion


    #region Private Serializable Fields
    [SerializeField] private TextMeshProUGUI _textID;
#endregion


#region Private Fields
    private TileContents _content;
    private Vector3 _tilePosition = new();
#endregion


#region MonoBehaviour CallBacks

#endregion


#region Private Methods

#endregion


#region Public Methods
    public void Initialize(TileContents content_, int tileNum_, Vector3 tilePosition_){
        _content = content_;
        // _textID.text = $"{tilePosition_.y},{tilePosition_.x}";
        _textID.text = _content.ToString().ToCharArray()[0].ToString();
        TilePosition = tilePosition_;
    }

    public void SetContent(TileContents content_)
    {
        _content = content_;
        _textID.text = _content.ToString().ToCharArray()[0].ToString();
    }
    #endregion
}