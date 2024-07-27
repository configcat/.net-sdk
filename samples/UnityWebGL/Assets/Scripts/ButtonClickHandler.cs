using System;
using ConfigCat.Client;
using TMPro;
using UnityEngine;

public class ButtonClickHandler : MonoBehaviour
{
    public TMP_Text Text;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void HandleClick()
    {
        try
        {
            var configCatClient = SingletonServices.Instance.ConfigCatClient;

            // Creating a user object to identify the user (optional)
            var user = new User("<SOME USERID>")
            {
                Country = "US",
                Email = "configcat@example.com",
                Custom =
                {
                    { "SubscriptionType", "Pro"},
                    { "Role", "Admin"},
                    { "version", "1.0.0" }
                }
            };

            var value = await configCatClient.GetValueAsync("isPOCFeatureEnabled", false, user);

            Text.SetText($"Value returned from ConfigCat: {value}");
            Text.gameObject.SetActive(true);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
