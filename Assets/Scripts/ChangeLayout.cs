using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeLayout : MonoBehaviour
{
    public enum Kartina{
        ld,
        rq,
        nn
    }

    public Kartina thisKartina = Kartina.ld;
    // no net
    RectTransform nn_bg;
    // Loading
    RectTransform ld_spinner, ld_logo, ld_bg;
    // Req
    RectTransform rq_logo, rq_bg, rq_text1, rq_text2, rq_btn1, rq_btn2, rq_btn1_text, rq_btn2_text;

    private IEnumerator SpinLoading()
    {
        while (true && thisKartina == Kartina.ld)
        {
            yield return new WaitForEndOfFrame();
            ld_spinner.Rotate(0f, 0f, 500f * Time.deltaTime);
        }
    }

    private void OnEnable()
    {
        if(thisKartina == Kartina.ld){
            ld_spinner = transform.GetChild(2).GetComponent<RectTransform>();
            ld_logo = transform.GetChild(1).GetComponent<RectTransform>();
            ld_bg = transform.GetChild(0).GetComponent<RectTransform>();
        }
        else if (thisKartina == Kartina.rq){
            rq_bg = transform.GetChild(0).GetComponent<RectTransform>();
            rq_logo = transform.GetChild(1).GetComponent<RectTransform>();
            rq_text1 = transform.GetChild(3).GetComponent<RectTransform>();
            rq_text2 = transform.GetChild(4).GetComponent<RectTransform>();
            rq_btn1 = transform.GetChild(5).GetComponent<RectTransform>();
            rq_btn2 = transform.GetChild(6).GetComponent<RectTransform>();
            rq_btn1_text = rq_btn1.GetChild(0).GetComponent<RectTransform>();
            rq_btn2_text = rq_btn2.GetChild(0).GetComponent<RectTransform>();
        }
        else if (thisKartina == Kartina.nn){
            nn_bg = transform.GetChild(0).GetComponent<RectTransform>();
        }

        StartCoroutine(SpinLoading());
        StartCoroutine(CheckOrientation());
    }

    ScreenOrientation _currentOrientation = ScreenOrientation.Portrait;

    IEnumerator CheckOrientation(){
        HandleOrientationChange();
        while(true){
            yield return new WaitForEndOfFrame();
            if(_currentOrientation != Screen.orientation)
                HandleOrientationChange();
        }
    }

    void HandleOrientationChange(){
        Debug.Log("Orientation changed to " + Screen.orientation);
        _currentOrientation = Screen.orientation;
        if(Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight){
            if(thisKartina == Kartina.ld){
                // Loading
                // BG
                // horizontal stretch
                ld_bg.anchorMin = new Vector2(0, 0.5f);
                ld_bg.anchorMax = new Vector2(1, 0.5f);
                ld_bg.sizeDelta = new Vector2(0, 7000);
                ld_bg.offsetMin = new Vector2(0, ld_bg.offsetMin.y);
                ld_bg.offsetMax = new Vector2(0, ld_bg.offsetMax.y);
                // logo
                ld_logo.localPosition = new Vector3(-650, 0, 0);
                // spinner
                ld_spinner.localPosition = new Vector3(650, 0, 0);
            }
            else if(thisKartina == Kartina.rq){
                // Notifications
                // BG
                // horizontal stretch
                rq_bg.anchorMin = new Vector2(0, 0.5f);
                rq_bg.anchorMax = new Vector2(1, 0.5f);
                rq_bg.sizeDelta = new Vector2(0, 7000);
                rq_bg.offsetMin = new Vector2(0, rq_bg.offsetMin.y);
                rq_bg.offsetMax = new Vector2(0, rq_bg.offsetMax.y);
                // text
                rq_text1.localPosition = new Vector3(0,0,965);
                // logo to the top
                rq_logo.anchorMin = new Vector2(0.5f, 1);
                rq_logo.anchorMax = new Vector2(0.5f, 1);
                rq_logo.pivot = new Vector2(0.5f, 0.5f);
                rq_logo.anchoredPosition = new Vector2(0, -613);   
                // logo size
                rq_logo.sizeDelta = new Vector2(1500, 1500);
                // text positions
                rq_text1.localPosition = new Vector2(0,58);
                rq_text2.localPosition = new Vector2(0,-231);
                // buttons positions
                rq_btn1.anchoredPosition = new Vector2(0, 564);
                rq_btn2.anchoredPosition = new Vector2(0, 242);
                // button height
                rq_btn1.sizeDelta = new Vector2(rq_btn1.sizeDelta.x, 200);
                rq_btn2.sizeDelta = new Vector2(rq_btn2.sizeDelta.x, 200);
                // change text size
                rq_text1.GetComponent<Text>().fontSize = 126;
                rq_text2.GetComponent<Text>().fontSize = 90;
                rq_btn1_text.GetComponent<Text>().fontSize = 90;
                rq_btn2_text.GetComponent<Text>().fontSize = 80;
                // btn1 and btn2 left and right positions
                float leftValue = 130f;
                float rightValue = 130f;
                if(Screen.orientation == ScreenOrientation.LandscapeLeft){
                    leftValue = 350f;
                    rightValue = 150f;
                }
                else if(Screen.orientation == ScreenOrientation.LandscapeRight){
                    leftValue = 150f;
                    rightValue = 350f;
                }
                // Stretch btn1 horizontally
                rq_btn1.anchorMin = new Vector2(0, rq_btn1.anchorMin.y);
                rq_btn1.anchorMax = new Vector2(1, rq_btn1.anchorMax.y);
                rq_btn1.offsetMin = new Vector2(leftValue, rq_btn1.offsetMin.y);
                rq_btn1.offsetMax = new Vector2(-rightValue, rq_btn1.offsetMax.y);
                // Stretch btn2 horizontally
                rq_btn2.anchorMin = new Vector2(0, rq_btn2.anchorMin.y);
                rq_btn2.anchorMax = new Vector2(1, rq_btn2.anchorMax.y);
                rq_btn2.offsetMin = new Vector2(leftValue, rq_btn2.offsetMin.y);
                rq_btn2.offsetMax = new Vector2(-rightValue, rq_btn2.offsetMax.y);
            }
            else if(thisKartina == Kartina.nn){
                nn_bg.anchorMin = new Vector2(0, 0.5f);
                nn_bg.anchorMax = new Vector2(1, 0.5f);
                nn_bg.sizeDelta = new Vector2(0, 7000);
                nn_bg.offsetMin = new Vector2(0, nn_bg.offsetMin.y);
                nn_bg.offsetMax = new Vector2(0, nn_bg.offsetMax.y);
            }
        }
        else if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown){
            if(thisKartina == Kartina.ld){
                // Loading
                // BG
                // vertical streatch
                ld_bg.anchorMin = new Vector2(0.5f, 0);
                ld_bg.anchorMax = new Vector2(0.5f, 1);
                ld_bg.sizeDelta = new Vector2(7000, 0);
                ld_bg.offsetMin = new Vector2(ld_bg.offsetMin.x, 0);
                ld_bg.offsetMax = new Vector2(ld_bg.offsetMax.x, 0);
                // logo
                ld_logo.localPosition = new Vector3(0, 200, 0);
                // spinner
                ld_spinner.localPosition = new Vector3(0, -494, 0);
            }
            else if (thisKartina == Kartina.rq){
                // Notifications
                // BG
                // vertical stretch
                rq_bg.anchorMin = new Vector2(0.5f, 0);
                rq_bg.anchorMax = new Vector2(0.5f, 1);
                rq_bg.sizeDelta = new Vector2(7000, 0);
                rq_bg.offsetMin = new Vector2(rq_bg.offsetMin.x, 0);
                rq_bg.offsetMax = new Vector2(rq_bg.offsetMax.x, 0);
                // text
                rq_text1.localPosition = new Vector3(0,-336,0);
                rq_text2.localPosition = new Vector3(0, -586, 0);
                // buttons
                rq_btn1.localPosition = new Vector2(0, -836);
                rq_btn2.localPosition = new Vector2(0, -1000);
                // logo
                rq_logo.anchorMin = new Vector2(0.5f, 1);
                rq_logo.anchorMax = new Vector2(0.5f, 1);
                rq_logo.pivot = new Vector2(0.5f, 0.5f);
                rq_logo.anchoredPosition = new Vector2(0, -613);
                // logo size
                rq_logo.sizeDelta = new Vector2(900, 900);
                // text positions
                rq_text1.localPosition = new Vector2(0,-300);
                rq_text2.localPosition = new Vector2(0,-510);
                // buttons positions
                rq_btn1.anchoredPosition = new Vector2(0, 360);
                rq_btn2.anchoredPosition = new Vector2(0, 160);
                // button height
                rq_btn1.sizeDelta = new Vector2(rq_btn1.sizeDelta.x, 160);
                rq_btn2.sizeDelta = new Vector2(rq_btn2.sizeDelta.x, 130);
                // change text size
                rq_text1.GetComponent<Text>().fontSize = 48;
                rq_text2.GetComponent<Text>().fontSize = 48;
                rq_btn1_text.GetComponent<Text>().fontSize = 48;
                rq_btn2_text.GetComponent<Text>().fontSize = 50;
                // btn1 and btn2 left and right positions
                float leftValue = 130f;
                float rightValue = 130f;
                // Stretch btn1 horizontally
                rq_btn1.anchorMin = new Vector2(0, rq_btn1.anchorMin.y);
                rq_btn1.anchorMax = new Vector2(1, rq_btn1.anchorMax.y);
                rq_btn1.offsetMin = new Vector2(leftValue, rq_btn1.offsetMin.y);
                rq_btn1.offsetMax = new Vector2(-rightValue, rq_btn1.offsetMax.y);
                // Stretch btn2 horizontally
                rq_btn2.anchorMin = new Vector2(0, rq_btn2.anchorMin.y);
                rq_btn2.anchorMax = new Vector2(1, rq_btn2.anchorMax.y);
                rq_btn2.offsetMin = new Vector2(leftValue, rq_btn2.offsetMin.y);
                rq_btn2.offsetMax = new Vector2(-rightValue, rq_btn2.offsetMax.y);
            }
            else if (thisKartina == Kartina.nn){
                nn_bg.anchorMin = new Vector2(0.5f, 0);
                nn_bg.anchorMax = new Vector2(0.5f, 1);
                nn_bg.sizeDelta = new Vector2(7000, 0);
                nn_bg.offsetMin = new Vector2(nn_bg.offsetMin.x, 0);
                nn_bg.offsetMax = new Vector2(nn_bg.offsetMax.x, 0);
            }
        }
    }
}
