using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirebaseController : MonoBehaviour
{

    public GameObject loginPanel, signupPanel, profilePanel, forgetPasswordPanel, notificationPanel, shopPanel;

    public TMP_InputField loginEmail, loginPassword, signupEmail, signupPassword, signupCPassword, signupUserName, forgetPassEmail;

    public TMP_Text notif_Title_Text, notif_Message_Text, profileUserName_Text, profileUserEmail_Text;

    public FirebaseDatabaseController dbController;

    bool isSignIn = false;

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                InitializeFirebase();

                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);
        shopPanel.SetActive(false);
    }

    public void OpenSignUpPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);
        shopPanel.SetActive(false);
    }

    public void OpenProfliePanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        forgetPasswordPanel.SetActive(false);
        shopPanel.SetActive(false);
    }

    public void OpenForgetPassPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(true);
        shopPanel.SetActive(false);
    }

    public void LoginUser()
    {
        if (string.IsNullOrEmpty(loginEmail.text) && string.IsNullOrEmpty(loginPassword.text))
        {
            ShowNotificationMessage("Error", "Fields Empty Please Input Details In All Fields.");
            return;
        }
        SignInUser(loginEmail.text, loginPassword.text);

    }

    public void SignUpUser()
    {
        if (string.IsNullOrEmpty(signupEmail.text) && string.IsNullOrEmpty(signupPassword.text) && string.IsNullOrEmpty(signupCPassword.text) && string.IsNullOrEmpty(signupUserName.text))
        {
            ShowNotificationMessage("Error", "Fields Empty Please Input Details In All Fields.");
            return;
        }

        CreateUser(signupEmail.text, signupPassword.text, signupUserName.text);
        OpenLoginPanel();
    }

    public void ForgetPass()
    {
        if (string.IsNullOrEmpty(forgetPassEmail.text))
        {
            ShowNotificationMessage("Error", "Forget Email Empty");
            return;
        }
        ForgetPasswordSubmit(forgetPassEmail.text);
    }

    private void ShowNotificationMessage(string title, string message)
    {
        notif_Title_Text.text = "" + title;
        notif_Message_Text.text = "" + message;

        notificationPanel.SetActive(true);
    }

    public void CloseNotfiPanel()
    {
        notif_Title_Text.text = "";
        notif_Message_Text.text = "";

        notificationPanel.SetActive(false);
    }

    public void LogOut()
    {
        auth.SignOut();
        Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(dbController.userId).Child("Status").SetValueAsync("Offline");
        profilePanel.SetActive(false);
        profileUserName_Text.text = "";
        profileUserEmail_Text.text = "";
        OpenLoginPanel();
    }

    void CreateUser(string email, string password, string Username)
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);

                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }

                return;
            }

            // Firebase user has been created.
            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(Username);
            if (dbController != null)
            {
                dbController.userId = result.User.UserId;   // 使用 Firebase UID 當 key
                dbController.dts = new DataToSave()
                {
                    UserName = Username,
                    TotalCoins = 0,   // 初始金幣
                    CrrLevel = 1,    // 初始等級
                    currentEquip = new EquipData()
                    {
                        hair = 0,
                        pants = 0,
                        shoes = 0,
                        face = 0,
                        other = -1
                    },

                    ownedItems = new OwnedItems()
                    {
                        hair = new List<int> { 0 },
                        pants = new List<int> { 0 },
                        shoes = new List<int> { 0 },
                        face = new List<int> { 0 },
                        other = new List<int> { -1 },
                        furniture = new List<int> { -1 } // 空的
                    },
                    Friends = new List<string>() { "init" },
                    FriendRequests = new FriendRequests()
                    {
                        Sent = new List<string>() { "init" },
                        Received = new List<string>() { "init" }
                    }

                };
                dbController.SaveDataFn();  // 呼叫存檔
                Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(dbController.userId).Child("Status").SetValueAsync("Offline");
                Debug.Log("✅ Initial user data saved to Realtime Database");
            }
            else
            {
                Debug.LogError("❌ dbController is null! Did you assign it in Inspector?");
            }
        });
    }


    public void SignInUser(string email, string password)
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }

                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
            profileUserName_Text.text = "" + result.User.DisplayName;
            profileUserEmail_Text.text = "" + result.User.Email;

            //Debug.Log("UserID: " + result.User.UserId);
            if (dbController != null)
            {
                Debug.Log("UserID: " + result.User.UserId);
                dbController.userId = result.User.UserId;  // 確保 userId 設定正確
                dbController.LoadDataFn();// 呼叫你的 Database 載入資料
                StartCoroutine(WaitAndStartFriendListener());
            }
            Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(result.User.UserId).Child("Status").SetValueAsync("Online");
            OpenProfliePanel();
        });
    }

    private System.Collections.IEnumerator WaitAndStartFriendListener()
    {
        // 等待 userId 被設定
        yield return new WaitUntil(() => dbController != null && !string.IsNullOrEmpty(dbController.userId));

        var friendSystem = FindObjectOfType<FriendSystemController>();
        if (friendSystem != null)
        {
            friendSystem.StartListeningForFriendRequests();
            Debug.Log($"🚀 Friend request listener started for {dbController.userId}");
        }
        else
        {
            Debug.LogWarning("⚠️ FriendSystemController not found in scene!");
        }
    }


    void InitializeFirebase()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                isSignIn = true;
            }
        }
    }

    void OnDestroy()
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    void UpdateUserProfile(string Username)
    {
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
            {
                DisplayName = Username,
                PhotoUrl = new System.Uri("https://example.com/jane-q-user/profile.jpg"),
            };
            user.UpdateUserProfileAsync(profile).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("User profile updated successfully.");
                ShowNotificationMessage("Alert", "Account Successfully Created!");
            });
        }
    }

    bool isSigned = false;
    void Update()
    {
        if (isSignIn)
        {
            if (isSigned)
            {
                isSigned = true;
                profileUserName_Text.text = "" + user.DisplayName;
                profileUserEmail_Text.text = "" + user.Email;
                OpenProfliePanel();
            }
        }
    }

    private static string GetErrorMessage(AuthError errorCode)
    {
        var message = "";
        switch (errorCode)
        {
            case AuthError.AccountExistsWithDifferentCredentials:
                message = "Account Not Exist";
                break;
            case AuthError.MissingPassword:
                message = "Missing Password";
                break;
            case AuthError.WeakPassword:
                message = "Password So Weak";
                break;
            case AuthError.WrongPassword:
                message = "Wrong Password";
                break;
            case AuthError.EmailAlreadyInUse:
                message = "Your Email Already In Use";
                break;
            case AuthError.InvalidEmail:
                message = "Your Email Ivalid";
                break;
            case AuthError.MissingEmail:
                message = "Your Email Missing";
                break;
            default:
                message = "Ivalid Error";
                break;
        }
        return message;
    }

    void ForgetPasswordSubmit(string forgetPasswordEmail)
    {
        auth.SendPasswordResetEmailAsync(forgetPasswordEmail).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmailAsync was canceled");
            }
            if (task.IsFaulted)
            {
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }
            }
            ShowNotificationMessage("Alert", "Successfully Send Email For Reset Password");
        });
    }
}
