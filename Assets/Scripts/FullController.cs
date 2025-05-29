using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FullController : MonoBehaviour
{
    [SerializeField] bool doForceOrientation = true;
    [SerializeField] string privacyLink;
    [SerializeField] GameObject MainMenu, ScoreCanvas, SettingsCanvas;
    [SerializeField] Color team1Color, team2Color;
    [SerializeField] Text Team1Name, Team2Name, Team1ScoreText, Team2ScoreText, Team1WinsText, Team2WinsText;
    [SerializeField] Text SelectedModeText;
    [SerializeField] Text GameTimeText;
    [SerializeField] TMP_InputField team1NameInput, team2NameInput;
    [SerializeField] TMP_Dropdown gameModeDropdown;
    [SerializeField] Slider gameTimeSlider;
    [SerializeField] GameObject PopupObject;
    [SerializeField] Text popupTitle, popupText;
    [SerializeField] Text RoundNumberText;
    [SerializeField] Text SettingsSliderCurrentValueText;
    string team1Name, team2Name;
    string gameMode;
    int Team1Score = 0, Team2Score = 0;
    int TotalTeam1Wins = 0, TotalTeam2Wins = 0;
    float _gameTime = 0;
    int GAME_TIME;
    bool _doCount = false, skipTimer = false, nextRoundStarted = false;
    int roundNumber;
    void Start()
    {
        OpenMenu();
        team1Name = GenerateRandomTeamName();
        team2Name = GenerateRandomTeamName();
        roundNumber = 1;
        gameMode = "volleyball";
        if(doForceOrientation){
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }
    }
    void _closeCanvases(){
        MainMenu.SetActive(false);
        ScoreCanvas.SetActive(false);
        SettingsCanvas.SetActive(false);
        PopupObject.SetActive(false);
        _doCount = false;
        ResetGame();
    }
    public void OpenPrivacy(){
        Application.OpenURL(privacyLink);
    }
    public void OpenMenu(){
        _closeCanvases();
        MainMenu.SetActive(true);
    }
    public void OpenScoreCanvas(){
        _closeCanvases();
        ScoreCanvas.SetActive(true);
        nextRoundStarted = true;
        skipTimer = false;
        _doCount = true;
    }
    public void OpenSettingsCanvas(){
        _closeCanvases();
        SettingsCanvas.SetActive(true);
    }
    void Update()
    {
        if(!skipTimer){
            _gameTime += Time.deltaTime;
        }
        if(!nextRoundStarted){
            return;
        }
        if(gameMode.ToLower() == "volleyball"){
            HandleVolleyball();
        }
        if(gameMode.ToLower() == "table tennis"){
            HandleTableTennis();
        }
        if(gameMode.ToLower() == "football"){
            HandleFootball();
        }
        UpdateUI();
    }
    public void OnTimerSliderValueChanged(){
        GAME_TIME = (int)gameTimeSlider.value * 60;
        SettingsSliderCurrentValueText.text = $"Current value: {gameTimeSlider.value} minutes";
    }
    void StartPopupActions(string title, string text){
        PopupObject.SetActive(true);
        popupTitle.text = title;
        popupText.text = text;
        skipTimer = true;
        nextRoundStarted = false;
    }
    public void ClosePopup(){
        ResetScore();
        ResetTimer();
        roundNumber += 1;
        PopupObject.SetActive(false);
        skipTimer = false;
        nextRoundStarted = true;
    }
    public void HandleVolleyball(){
        Debug.Log("Volleyball");
        if(Team1Score >= 24 && Team2Score >= 24){
            if(Team1Score - Team2Score == 2){
                StartPopupActions("Team 1 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 1 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam1Win();
                nextRoundStarted = false;
            }
            else if(Team2Score - Team1Score == 2){
                StartPopupActions("Team 2 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 2 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam2Win();
                nextRoundStarted = false;
            }
        }
        else if (Team1Score == 25 && Team2Score < 24){
            StartPopupActions("Team 1 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 1 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
            AddTeam1Win();
            nextRoundStarted = false;
        }
        else if (Team1Score < 24 && Team2Score == 25){
            StartPopupActions("Team 2 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 2 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
            AddTeam2Win();
            nextRoundStarted = false;
        }
    }
    public void HandleTableTennis(){
        Debug.Log("Table Tennis");
        if(Team1Score >= 10 && Team2Score >= 10){
            if(Team1Score - Team2Score == 2){
                StartPopupActions("Team 1 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 1 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam1Win();
                nextRoundStarted = false;
            }
            else if(Team2Score - Team1Score == 2){
                StartPopupActions("Team 2 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 2 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam2Win();
                nextRoundStarted = false;
            }
        }
        else if (Team1Score == 11 && Team2Score < 10){
            StartPopupActions("Team 1 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 1 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
            AddTeam1Win();
            nextRoundStarted = false;
        }
        else if (Team1Score < 10 && Team2Score == 11){
            StartPopupActions("Team 2 won", $"Game lasted {ConvertFromTimeToString(_gameTime)}\nTeam 2 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
            AddTeam2Win();
            nextRoundStarted = false;
        }
    }
    public void HandleFootball(){
        Debug.Log("Football");
        if(_gameTime >= GAME_TIME){
            if(Team1Score > Team2Score){
                StartPopupActions("Team 1 won", $"Team 1 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam1Win();
                nextRoundStarted = false;
            }
            else if(Team2Score > Team1Score){
                StartPopupActions("Team 2 won", $"Team 2 won in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
                AddTeam2Win();
                nextRoundStarted = false;
            }
            else{
                StartPopupActions("Draw", $"Draw in round {roundNumber}\nScore: {Team1Score} - {Team2Score}");
            }
        }
    }
    public void ResetTimer() => _gameTime = 0;
    public void UpdateGameMode() => gameMode = gameModeDropdown.options[gameModeDropdown.value].text;
    public void UpdateTeam1Name() => team1Name = team1NameInput.text;
    public void UpdateTeam2Name() => team2Name = team2NameInput.text;
    public void AddPointTeam1() => Team1Score++;
    public void AddPointTeam2() => Team2Score++;
    public void AddTeam1Win() => TotalTeam1Wins++;
    public void MinusTeam1Win() => TotalTeam1Wins--;
    public void MinusTeam2Win() => TotalTeam2Wins--;
    public void MinusPointTeam1() => Team1Score--;
    public void MinusPointTeam2() => Team2Score--;
    public void AddTeam2Win() => TotalTeam2Wins++;
    public void ResetScore(){
        Team1Score = 0;
        Team2Score = 0;
    }
    public void ResetGame(){
        Team1Score = 0;
        Team2Score = 0;
        TotalTeam1Wins = 0;
        TotalTeam2Wins = 0;
        _gameTime = 0;
        roundNumber = 1;
        // team1Name = GenerateRandomTeamName();
        // team2Name = GenerateRandomTeamName();
        nextRoundStarted = true;
        skipTimer = false;
        _doCount = true;
    }
    public void UpdateUI(){
        Team1Name.text = team1Name;
        Team2Name.text = team2Name;
        Team1ScoreText.text = Team1Score.ToString();
        Team2ScoreText.text = Team2Score.ToString();
        Team1WinsText.text = TotalTeam1Wins.ToString();
        Team2WinsText.text = TotalTeam2Wins.ToString();
        SelectedModeText.text = $"Game mode:\n{gameMode}";
        GameTimeText.text = ConvertFromTimeToString(_gameTime);
        RoundNumberText.text = $"Round {roundNumber}";
    }
    string GenerateRandomTeamName()
    {
        string[] adjectives = {
            "Red", "Blue", "Green", "Golden", "Silver", "Fierce", "Mighty", "Swift", 
            "Brave", "Dark", "Light", "Wild", "Royal", "Ancient", "Mystic", "Epic",
            "Thunder", "Fire", "Ice", "Storm", "Shadow", "Iron", "Steel", "Diamond",
            "Cosmic", "Galactic", "Solar", "Lunar", "Raging", "Silent", "Flying", "Running",
            "Hidden", "Secret", "Lucky", "Magic", "Electric", "Atomic", "Nuclear", "Titan"
        };
        string[] nouns = {
            "Dragons", "Lions", "Tigers", "Bears", "Wolves", "Eagles", "Hawks", "Falcons",
            "Sharks", "Raptors", "Knights", "Warriors", "Giants", "Titans", "Legends", "Heroes",
            "Stars", "Comets", "Meteors", "Planets", "Storms", "Flames", "Blaze", "Lightning",
            "Phantoms", "Ghosts", "Specters", "Ninjas", "Samurai", "Vikings", "Pirates", "Rebels",
            "Outlaws", "Bandits", "Raiders", "Storm", "Tornadoes", "Hurricanes", "Cyclones", "Wizards"
        };
        return $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
    }
    string ConvertFromTimeToString(float time){
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}
