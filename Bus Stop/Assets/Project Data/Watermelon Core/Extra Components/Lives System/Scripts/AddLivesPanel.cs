using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;

namespace Watermelon
{
    public class AddLivesPanel : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] Vector3 hidePos;
        private Vector3 showPos;

        [SerializeField] Image backgroundImage;

        [SerializeField] Button button;
        [SerializeField] Button closeButton;

        [SerializeField] TMP_Text livesAmountText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] AudioClip lifeRecievedAudio;

        Color backColor;

        private static int openedStack;
        public static bool IsPanelOpened => openedStack != 0;

        public SimpleBoolCallback OnPanelClosedCallback;
        public string clickid;
        private StarkAdManager starkAdManager;
        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            backColor = backgroundImage.color;
            showPos = panel.anchoredPosition;

            button.onClick.AddListener(OnButtonClick);
            closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            LivesManager.AddPanel(this);
        }

        private void OnDisable()
        {
            LivesManager.RemovePanel(this);
        }

        public void Show(SimpleBoolCallback onPanelClosed = null)
        {
            gameObject.SetActive(true);

            backgroundImage.color = Color.clear;
            backgroundImage.DOColor(backColor, 0.3f);

            panel.anchoredPosition = hidePos;
            panel.DOAnchoredPosition(showPos, 0.3f).SetEasing(Ease.Type.SineOut);

            openedStack++;

            OnPanelClosedCallback = onPanelClosed;
        }

        public void Hide()
        {
            backgroundImage.DOColor(Color.clear, 0.3f);
            panel.DOAnchoredPosition(hidePos, 0.3f).SetEasing(Ease.Type.SineIn).OnComplete(() => gameObject.SetActive(false));

            openedStack--;

            OnPanelClosedCallback?.Invoke(false);
        }

        public void OnButtonClick()
        {
            ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    LivesManager.AddLife();

                    if (lifeRecievedAudio != null)
                        AudioController.PlaySound(lifeRecievedAudio);

                    OnPanelClosedCallback?.Invoke(true);
                    OnPanelClosedCallback = null;
                    Hide();


                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
            
            
        }

        public void SetLivesCount(int count)
        {
            livesAmountText.text = count.ToString();
        }

        public void SetTime(string time)
        {
            timeText.text = time;
        }
        public void getClickid()
        {
            var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
            if (launchOpt.Query != null)
            {
                foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                    if (kv.Value != null)
                    {
                        Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                        if (kv.Key.ToString() == "clickid")
                        {
                            clickid = kv.Value.ToString();
                        }
                    }
                    else
                    {
                        Debug.Log(kv.Key + "<-参数-> " + "null ");
                    }
            }
        }

        public void apiSend(string eventname, string clickid)
        {
            TTRequest.InnerOptions options = new TTRequest.InnerOptions();
            options.Header["content-type"] = "application/json";
            options.Method = "POST";

            JsonData data1 = new JsonData();

            data1["event_type"] = eventname;
            data1["context"] = new JsonData();
            data1["context"]["ad"] = new JsonData();
            data1["context"]["ad"]["callback"] = clickid;

            Debug.Log("<-data1-> " + data1.ToJson());

            options.Data = data1.ToJson();

            TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
               response => { Debug.Log(response); },
               response => { Debug.Log(response); });
        }


        /// <summary>
        /// </summary>
        /// <param name="adId"></param>
        /// <param name="closeCallBack"></param>
        /// <param name="errorCallBack"></param>
        public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
        {
            starkAdManager = StarkSDK.API.GetStarkAdManager();
            if (starkAdManager != null)
            {
                starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
            }
        }
    }
}