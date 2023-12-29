using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameBehaviour : MonoBehaviour
{
    #region Variables
    //Serialized variables
    [SerializeField] MapBehaviour m_Map;

    [Header("UI - information")]
    [SerializeField] private TextMeshProUGUI m_ErrorMessage;
    [SerializeField] private TextMeshProUGUI m_TimerText;
    [SerializeField] private TextMeshProUGUI m_MineCounterText;

    [Header("UI - input fields")]
    [SerializeField] private TextMeshProUGUI m_WidthInputField;
    [SerializeField] private TextMeshProUGUI m_HeightInputField;
    [SerializeField] private TextMeshProUGUI m_MineCountInputField;

    [Header("UI - smiley")]
    [SerializeField] private Sprite m_GameNeutralSmiley;
    [SerializeField] private Sprite m_GameWonSmiley;
    [SerializeField] private Sprite m_GameLossSmiley;
    [SerializeField] private Image m_GameSmileyRepresentation;

    //regular variables
    private int m_UpdatedMapWidth;
    private int m_UpdatedMapHeight;
    private int m_UpdatedMineCount;

    private bool m_GameOver = false;
    private float m_Timer = 0f;
    #endregion

    #region LifeTime
    private void Start()
    {
        m_UpdatedMapWidth = m_Map.MapWidth;
        m_UpdatedMapHeight = m_Map.MapHeight;
        m_UpdatedMineCount = m_Map.MineCount;

        m_WidthInputField.text = m_UpdatedMapWidth.ToString();
        m_HeightInputField.text = m_UpdatedMapHeight.ToString();
        m_MineCountInputField.text = m_UpdatedMineCount.ToString();

        m_Map.GameOverEvent.AddListener(GameOver);

        //start the game
        StartGame();
    }

    private void Update()
    {
        m_MineCounterText.text = m_Map.MineCounter.ToString();

        if (m_GameOver)
            return;

        if (m_Map.FirstClickHappened)
        {
            m_Timer += Time.deltaTime;
            m_TimerText.text = m_Timer.ToString("F2");
        }

        if (Input.GetMouseButtonDown(0))//left mouse click occured
            m_Map.HandleLeftMouseClick();
        else if (Input.GetMouseButtonDown(1))//right mouse click occured
            m_Map.SetFlag();
    }
    #endregion

    #region UI accessed functions
    public void StartGame()
    {
        if (m_UpdatedMineCount > ((m_UpdatedMapWidth * m_UpdatedMapHeight) - 9))//Our first click is a 0 tile which means at least 9 tiles can't be a mine
        {
            LogError("There are to many bombs for the size of the map");
            return;
        }

        //setup camera size
        Camera camera = Camera.main;
        float minSizeForHeight = (m_UpdatedMapHeight / 2f) * 1.15f; //Divided by 2 because 2 cells can fit into one orthographicSize height multiplied by a ratio that looks good with current UI setup
        float minSizeForWidth = (m_UpdatedMapWidth / 4f) * 1.5f; //Divided by 4 because 4 cells can fit into one orthographicSize width multiplied by a ratio that looks good with current UI setup

        //Use the heighest size number to make sure the map always fits onto the screen
        camera.orthographicSize = Mathf.Max(minSizeForHeight, minSizeForWidth);

        //setup our location to always start from the bottom left of our camera
        float screenOffsetRatio = 0.05f; //Just a small value that looks good
        Vector3 screenPos = new Vector3(camera.pixelWidth * screenOffsetRatio, camera.pixelHeight * screenOffsetRatio, 10f);//
        m_Map.transform.position = camera.ScreenToWorldPoint(screenPos);

        //Setup variables correctly
        m_GameSmileyRepresentation.sprite = m_GameNeutralSmiley;
        m_ErrorMessage.gameObject.SetActive(false);
        m_GameOver = false;
        m_Timer = 0f;
        m_TimerText.text = m_Timer.ToString("F2");

        //setup the map
        m_Map.SetupMap(m_UpdatedMineCount, m_UpdatedMapWidth, m_UpdatedMapHeight);
    }

    public void UpdateGameWidth(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMapWidth = temp;
    }

    public void UpdateGameHeight(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMapHeight = temp;
    }

    public void UpdateMineCounter(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMineCount = temp;
    }
    #endregion

    #region Core Functionality
    private void GameOver()
    {
        m_GameOver = true;

        //set smiley correctly
        if (m_Map.HasWon)
            m_GameSmileyRepresentation.sprite = m_GameWonSmiley;
        else
            m_GameSmileyRepresentation.sprite = m_GameLossSmiley;
    }
    #endregion

    #region helperFunctions
    private int CheckInputFieldDataForValidNumber(string value)
    {
        int newIntValue = -1;
        int.TryParse(value, out newIntValue);

        if (newIntValue <= 0)
            LogError("Invalid input given, the data has to be a number larger than 0");
        else
            m_ErrorMessage.gameObject.SetActive(false);

        return newIntValue;
    }

    private void LogError(string message)
    {
        m_ErrorMessage.text = message;
        m_ErrorMessage.gameObject.SetActive(true);
    }
    #endregion
}