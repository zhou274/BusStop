using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using System.Collections.Generic;

namespace Watermelon
{
    public class PUUIBehavior : MonoBehaviour
    {
        [Group("Refs")]
        [SerializeField] Image backgroundImage;

        [Group("Refs")]
        [SerializeField] Image iconImage;

        [Group("Refs")]
        [SerializeField] GameObject amountContainerObject;

        [Group("Refs")]
        [SerializeField] TextMeshProUGUI amountText;

        [Group("Refs")]
        [SerializeField] GameObject amountPurchaseObject;

        [Group("Refs")]
        [SerializeField] GameObject busyStateVisualsObject;

        [Space]
        [SerializeField] GameObject timerObject;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] Image timerBackground;

        [Space]
        [SerializeField] SimpleBounce bounce;

        protected PUBehavior behavior;
        public PUBehavior Behavior => behavior;

        protected PUSettings settings;
        public PUSettings Settings => settings;

        private Button button;

        private bool isTimerActive;
        private Coroutine timerCoroutine;
        public string clickid;
        private StarkAdManager starkAdManager;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => OnButtonClicked());
        }

        public void Initialise(PUBehavior powerUpBehavior)
        {
            behavior = powerUpBehavior;
            settings = powerUpBehavior.Settings;

            ApplyVisuals();

            Redraw();

            bounce.Initialise(transform);

            gameObject.SetActive(false);
        }

        protected virtual void ApplyVisuals()
        {
            iconImage.sprite = settings.Icon;
            iconImage.color = Color.white;

            backgroundImage.color = settings.BackgroundColor;
            timerBackground.color = settings.BackgroundColor.SetAlpha(0.7f);
        }

        public void Activate()
        {
            gameObject.SetActive(true);

            transform.localScale = Vector3.zero;
            transform.DOScale(1.0f, 0.3f).SetEasing(Ease.Type.BackOut);

            Redraw();
        }

        private IEnumerator TimerCoroutine(PUTimer timer)
        {
            isTimerActive = true;

            timerObject.SetActive(true);
            timerBackground.fillAmount = 1.0f;
            timerText.text = timer.Seconds;

            iconImage.color = new Color(1, 1, 1, 0.3f);

            while (timer.IsActive)
            {
                yield return null;
                yield return null;

                timerBackground.fillAmount = 1.0f - timer.State;
                timerText.text = timer.Seconds;

                if (timerBackground.fillAmount <= 0.0f)
                    break;
            }

            timerObject.SetActive(false);
            iconImage.color = Color.white;

            isTimerActive = false;
        }

        public void OnButtonClicked()
        {
            ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    if (!behavior.IsBusy)
                    {
                        AudioController.PlaySound(AudioController.Sounds.buttonSound);

                        bounce.Bounce();

                        PUController.UsePowerUp(settings.Type);
                    }


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

        public void Redraw()
        {
            int amount = settings.Save.Amount;
            if (amount > 0)
            {
                //amountContainerObject.SetActive(true);
                amountPurchaseObject.SetActive(false);

                amountText.text = amount.ToString();
            }
            else
            {
                amountContainerObject.SetActive(false);
                //amountPurchaseObject.SetActive(true);
            }

            PUTimer timer = behavior.GetTimer();
            if (!isTimerActive)
            {
                if (timer != null)
                {
                    timerCoroutine = StartCoroutine(TimerCoroutine(timer));
                }
            }

            if (settings.VisualiseActiveState)
                RedrawBusyVisuals(behavior.IsBusy);

            behavior.OnRedrawn();
        }

        protected virtual void RedrawBusyVisuals(bool state)
        {
            busyStateVisualsObject.SetActive(behavior.IsBusy);
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
