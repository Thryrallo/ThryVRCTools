using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if VRC_SDK_EXISTS
using VRC.Core;

namespace Thry
{
    public class AS_Main
    {
        private static string[] a_usernames;
        private static string[] p_usernames;
        private static string[] p_passwords;
        private static int i_user_index = 0;
        private static int i_add_index;

        private static void Init()
        {
            p_usernames = LoadArrayFromEditorPrefs<string>("vrc_usernames");
            p_passwords = LoadArrayFromEditorPrefs<string>("vrc_passwords");
            InitAUsernames();
            i_user_index = EditorPrefs.GetInt("vrc_selected_user");
        }

        private static void InitAUsernames()
        {
            a_usernames = new string[p_usernames.Length];
            p_usernames.CopyTo(a_usernames, 0);
            a_usernames = AddElementToArray(p_usernames, "");
            a_usernames = AddElementToArray(a_usernames, "Add");
            i_add_index = a_usernames.Length - 2;
        }

        public static string[] usernames
        {
            get
            {
                if (a_usernames == null)
                    Init();
                return a_usernames;
            }
        }

        public static int selected_user_index
        {
            get
            {
                if (a_usernames == null)
                    Init();
                return i_user_index;
            }
            set
            {
                if (value != i_user_index && value < add_index)
                {
                    EditorPrefs.SetInt("vrc_selected_user", value);
                    i_user_index = value;
                    Logout(false);
                    Login();
                }
                i_user_index = value;
            }
        }

        public static string GetUsername()
        {
            if (selected_user_index < add_index)
                return usernames[selected_user_index];
            return adding_username;
        }

        public static string GetPassword()
        {
            if (selected_user_index < add_index)
                return p_passwords[selected_user_index];
            return adding_password;
        }

        public static bool IsSavedUser()
        {
            return selected_user_index < add_index;
        }

        public static int add_index
        {
            get
            {
                if (a_usernames == null)
                    Init();
                return i_add_index;
            }
        }

        private static t[] LoadArrayFromEditorPrefs<t>(string name)
        {
            t[] array = new t[EditorPrefs.GetInt(name,0)];
            for (int i = 0; i < array.Length; i++)
            {
                if (typeof(t) == typeof(string))
                    array[i] = (t)(object)EditorPrefs.GetString(name + "#" + i,"");
                else if (typeof(t) == typeof(float))
                    array[i] = (t)(object)EditorPrefs.GetFloat(name + "#" + i,0);
                else if (typeof(t) == typeof(int))
                    array[i] = (t)(object)EditorPrefs.GetInt(name + "#" + i,0);
                else if (typeof(t) == typeof(bool))
                    array[i] = (t)(object)EditorPrefs.GetBool(name + "#" + i,false);
            }
            return array;
        }

        private static void SaveArrayToEditorPrefs<t>(string name, t[] array)
        {
            EditorPrefs.SetInt(name, array.Length);
            for(int i = 0; i < array.Length; i++)
            {
                if (typeof(t) == typeof(string))
                    EditorPrefs.SetString(name + "#" + i, (string)(object)array[i]);
                else if (typeof(t) == typeof(float))
                    EditorPrefs.SetFloat(name + "#" + i, (float)(object)array[i]);
                else if (typeof(t) == typeof(int))
                    EditorPrefs.SetInt(name + "#" + i, (int)(object)array[i]);
                else if (typeof(t) == typeof(bool))
                    EditorPrefs.SetBool(name + "#" + i, (bool)(object)array[i]);
            }
        }

        private static void DeleteEditorPrefsArray(string name)
        {
            int length = EditorPrefs.GetInt(name, 0);
            for (int i = 0; i < length; i++)
            {
                EditorPrefs.DeleteKey(name + "#" + i);
            }
            EditorPrefs.DeleteKey(name);
        }

        private static string[] AddElementToArray(string[] array, string new_element)
        {
            string[] new_array = new string[array.Length + 1];
            array.CopyTo(new_array, 0);
            new_array[array.Length] = new_element;
            return new_array;
        }

        public static string[] RemoveElementFromArray(string[] array, int index)
        {
            string[] new_array = new string[array.Length - 1];
            int add_i = 0;
            for (int i = 0; i < new_array.Length; i++)
            {
                if (i != index)
                    new_array[add_i++] = array[i];
            }
            return new_array;
        }

        public static void Login()
        {
            AttemptLogin();
        }

        public static void Logout(bool set_add_index)
        {
            signingIn = false;
            if(set_add_index)
                selected_user_index = add_index+1;
            EditorPrefs.SetString("sdk#username", null);
            EditorPrefs.SetString("sdk#password", null);
            VRC.Tools.ClearCookies();
            APIUser.Logout();
        }

        public static void Remove()
        {
            ClearOldPrefs();
            p_usernames = RemoveElementFromArray(p_usernames, selected_user_index);
            p_passwords = RemoveElementFromArray(p_passwords, selected_user_index);
            InitAUsernames();
            SaveArrayToEditorPrefs<string>("vrc_usernames", p_usernames);
            SaveArrayToEditorPrefs<string>("vrc_passwords", p_passwords);
            Logout(true);
        }

        private static void ClearOldPrefs()
        {
            DeleteEditorPrefsArray("vrc_usernames");
            DeleteEditorPrefsArray("vrc_passwords");
        }

        private static void SaveCredentials(string username, string password)
        {
            ClearOldPrefs();
            p_usernames = AddElementToArray(p_usernames, username);
            p_passwords = AddElementToArray(p_passwords, password);
            InitAUsernames();
            i_user_index = p_usernames.Length - 1;
            EditorPrefs.SetInt("vrc_selected_user", i_user_index);
            SaveArrayToEditorPrefs<string>("vrc_usernames", p_usernames);
            SaveArrayToEditorPrefs<string>("vrc_passwords", p_passwords);
        }

        private static string adding_username = "";
        private static string adding_password = "";
        public static void AddAccount(string username, string password)
        {
            adding_username = username;
            adding_password = password;
            //test credentials
            AttemptLogin(username, password, delegate()
            {
                SaveCredentials(username, password);
                AS_Window.RepaintActiveWindow();
            });
        }

        private static void OnAuthenticationCompleted()
        {
            AttemptLogin();
        }

        static string clientInstallPath;
        public static bool signingIn = false;
        static string error = null;

        public static System.Action onAuthenticationVerifiedAction;

        private static void AttemptLogin(string username = null, string password=null, Action successcallback = null)
        {
            if (selected_user_index >= add_index && (username == null || password == null))
                return;
            if (username == null)
                username = usernames[selected_user_index];
            if (password == null)
                password = p_passwords[selected_user_index];

            signingIn = true;
            APIUser.Login(username, password,
                delegate (ApiModelContainer<APIUser> c)
                {
                    APIUser user = c.Model as APIUser;
                    if (c.Cookies.ContainsKey("auth"))
                        ApiCredentials.Set(user.username, username, "vrchat", c.Cookies["auth"]);
                    else
                        ApiCredentials.SetHumanName(user.username);
                    signingIn = false;
                    error = null;
                    EditorPrefs.SetString("sdk#username", username);
                    EditorPrefs.SetString("sdk#password", password);
                    AnalyticsSDK.LoggedInUserChanged(user);

                    if (!APIUser.CurrentUser.canPublishAllContent)
                    {
                        if (UnityEditor.SessionState.GetString("HasShownContentPublishPermissionsDialogForUser", "") != user.id)
                        {
                            UnityEditor.SessionState.SetString("HasShownContentPublishPermissionsDialogForUser", user.id);
                            VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                        }
                    }
;
                    if (successcallback != null)
                        successcallback.Invoke();
                    AS_Window.RepaintActiveWindow();
                },
                delegate (ApiModelContainer<APIUser> c)
                {
                    Logout(false);
                    error = c.Error;
                    VRC.Core.Logger.Log("Error logging in: " + error);
                    AS_Window.RepaintActiveWindow();
                },
                delegate (ApiModelContainer<API2FA> c)
                {
                    if (c.Cookies.ContainsKey("auth"))
                        ApiCredentials.Set(username, username, "vrchat", c.Cookies["auth"]);
                    AS_Window.showTwoFactorAuthenticationEntry = true;
                    onAuthenticationVerifiedAction = OnAuthenticationCompleted;
                    AS_Window.RepaintActiveWindow();
                }
            );
        }

    }

    public class AS_Window : EditorWindow
    {
        [MenuItem("Thry/VRC/Account Switcher")]
        [MenuItem("VRChat SDK/Account Switcher",false,650)]
        static void Init()
        {
            window = (AS_Window)EditorWindow.GetWindow(typeof(AS_Window));
            window.titleContent = new GUIContent("VRC Account Switcher");
            window.Show();
        }

        static AS_Window window;

        const int ACCOUNT_LOGIN_BORDER_SPACING = 20;

        static string username = null;
        static string password = null;

        public static bool _showTwoFactorAuthenticationEntry = false;

        public static void RepaintActiveWindow()
        {
            window = GetWindow<AS_Window>();
            if (window != null)
                window.Repaint();
        }

        private void InitResources()
        {
            if (warningIconGraphic == null)
                warningIconGraphic = Resources.Load("2FAIcons/SDK_Warning_Triangle_icon") as Texture2D;
        }

        private void OnFocus()
        {
            if(AS_Main.selected_user_index < AS_Main.add_index && !AS_Main.signingIn && !APIUser.IsLoggedInWithCredentials)
            {
                AS_Main.Login();
            }
        }

        private void OnGUI()
        {
            InitResources();

            EditorGUI.BeginChangeCheck();
            AS_Main.selected_user_index = EditorGUILayout.Popup(AS_Main.selected_user_index, AS_Main.usernames);
            if (EditorGUI.EndChangeCheck())
            {
                checkingCode = false;
                showTwoFactorAuthenticationEntry = false;
            }

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(ACCOUNT_LOGIN_BORDER_SPACING);
            GUILayout.BeginVertical("Account", "window", GUILayout.Height(150), GUILayout.Width(340));

            if (AS_Main.signingIn)
                EditorGUILayout.LabelField("Signing in as " + AS_Main.GetUsername() + ".");
            else if (AS_Main.selected_user_index >= AS_Main.add_index)
                AddAccountGui();
            else
                AccountGUI();

            if (showTwoFactorAuthenticationEntry)
            {
                OnTwoFactorAuthenticationGUI();
            }

            GUILayout.EndVertical();
            GUILayout.Space(ACCOUNT_LOGIN_BORDER_SPACING);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void AddAccountGui()
        {
            VRCSdkControlPanel.InitAccount();

            username = EditorGUILayout.TextField("Username", username);
            password = EditorGUILayout.PasswordField("Password", password);

            if (GUILayout.Button("Add Account"))
                AS_Main.AddAccount(username,password);
        }

        private void AccountGUI()
        {
            if (APIUser.IsLoggedInWithCredentials)
                OnCreatorStatusGUI();
            else
                NotLoggedInGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label("");

            if(APIUser.IsLoggedInWithCredentials)
            {
                if (GUILayout.Button("Logout"))
                    AS_Main.Logout(true);
            }
            else
            {
                if (GUILayout.Button("Login"))
                    AS_Main.Login();
            }
            EditorGUILayout.Separator();
            if (GUILayout.Button("Remove"))
            {
                AS_Main.Remove();
            }
            GUILayout.Label("");
            GUILayout.EndHorizontal();
        }

        static void OnCreatorStatusGUI()
        {
            EditorGUILayout.LabelField("Logged in as:", APIUser.CurrentUser.displayName);

            if (SDKClientUtilities.IsInternalSDK())
                EditorGUILayout.LabelField("Developer Status: ", APIUser.CurrentUser.developerType.ToString());

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("World Creator Status: ", APIUser.CurrentUser.canPublishWorlds ? "Allowed to publish worlds" : "Not yet allowed to publish worlds");
            EditorGUILayout.LabelField("Avatar Creator Status: ", APIUser.CurrentUser.canPublishAvatars ? "Allowed to publish avatars" : "Not yet allowed to publish avatars");
            EditorGUILayout.EndVertical();

            if (!APIUser.CurrentUser.canPublishAllContent)
            {
                if (GUILayout.Button("More Info..."))
                {
                    VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                }
            }


            EditorGUILayout.EndHorizontal();
        }

        static void NotLoggedInGUI()
        {
            EditorGUILayout.LabelField("Selected user: ", AS_Main.usernames[AS_Main.selected_user_index]);

            EditorGUILayout.HelpBox("Login failed.", MessageType.Error);
        }

        private const string TWO_FACTOR_AUTHENTICATION_HELP_URL = "https://docs.vrchat.com/docs/setup-2fa";

        private const string ENTER_2FA_CODE_TITLE_STRING = "Enter a numeric code from your authenticator app (or one of your saved recovery codes).";
        private const string ENTER_2FA_CODE_LABEL_STRING = "Code:";

        private const string CHECKING_2FA_CODE_STRING = "Checking code...";
        private const string ENTER_2FA_CODE_INVALID_CODE_STRING = "Invalid Code";

        private const string ENTER_2FA_CODE_VERIFY_STRING = "Verify";
        private const string ENTER_2FA_CODE_CANCEL_STRING = "Cancel";
        private const string ENTER_2FA_CODE_HELP_STRING = "Help";

        private const int WARNING_ICON_SIZE = 60;
        private const int WARNING_FONT_HEIGHT = 18;

        static private Texture2D warningIconGraphic;

        static bool entered2faCodeIsInvalid;
        static bool authorizationCodeWasVerified;

        static private int previousAuthenticationCodeLength = 0;
        static bool checkingCode;
        static string authenticationCode = "";

        public static bool showTwoFactorAuthenticationEntry
        {
            get
            {
                return _showTwoFactorAuthenticationEntry;
            }
            set
            {
                _showTwoFactorAuthenticationEntry = value;
                if (!_showTwoFactorAuthenticationEntry && !authorizationCodeWasVerified)
                    AS_Main.Logout(false);
            }
        }


        static void OnTwoFactorAuthenticationGUI()
        {
            const int ENTER_2FA_CODE_BORDER_SIZE = 20;
            const int ENTER_2FA_CODE_BUTTON_WIDTH = 260;
            const int ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH = ENTER_2FA_CODE_BUTTON_WIDTH / 2;
            const int ENTER_2FA_CODE_ENTRY_REGION_WIDTH = 130;
            const int ENTER_2FA_CODE_MIN_WINDOW_WIDTH = ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH + ENTER_2FA_CODE_ENTRY_REGION_WIDTH + (ENTER_2FA_CODE_BORDER_SIZE * 3);

            bool isValidAuthenticationCode = IsValidAuthenticationCodeFormat();


            // Invalid code text
            if (entered2faCodeIsInvalid)
            {
                GUIStyle s = new GUIStyle(EditorStyles.label);
                s.alignment = TextAnchor.UpperLeft;
                s.normal.textColor = Color.red;
                s.fontSize = WARNING_FONT_HEIGHT;
                s.padding = new RectOffset(0, 0, (WARNING_ICON_SIZE - s.fontSize) / 2, 0);
                s.fixedHeight = WARNING_ICON_SIZE;

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                var textDimensions = s.CalcSize(new GUIContent(ENTER_2FA_CODE_INVALID_CODE_STRING));
                GUILayout.Label(new GUIContent(warningIconGraphic), GUILayout.Width(WARNING_ICON_SIZE), GUILayout.Height(WARNING_ICON_SIZE));
                EditorGUILayout.LabelField(ENTER_2FA_CODE_INVALID_CODE_STRING, s, GUILayout.Width(textDimensions.x));
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (checkingCode)
            {
                // Display checking code message
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUIStyle s = new GUIStyle(EditorStyles.label);
                s.alignment = TextAnchor.MiddleCenter;
                s.fixedHeight = WARNING_ICON_SIZE;
                EditorGUILayout.LabelField(CHECKING_2FA_CODE_STRING, s, GUILayout.Height(WARNING_ICON_SIZE));
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
                GUILayout.FlexibleSpace();
                GUIStyle titleStyle = new GUIStyle(EditorStyles.label);
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.wordWrap = true;
                EditorGUILayout.LabelField(ENTER_2FA_CODE_TITLE_STRING, titleStyle, GUILayout.Width(ENTER_2FA_CODE_MIN_WINDOW_WIDTH - (2 * ENTER_2FA_CODE_BORDER_SIZE)), GUILayout.Height(WARNING_ICON_SIZE), GUILayout.ExpandHeight(true));
                GUILayout.FlexibleSpace();
                GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
            GUILayout.FlexibleSpace();
            Vector2 size = EditorStyles.boldLabel.CalcSize(new GUIContent(ENTER_2FA_CODE_LABEL_STRING));
            EditorGUILayout.LabelField(ENTER_2FA_CODE_LABEL_STRING, EditorStyles.boldLabel, GUILayout.MaxWidth(size.x));
            authenticationCode = EditorGUILayout.TextField(authenticationCode);

            string auth_username = username;
            string auth_password = password;
            if(AS_Main.IsSavedUser())
            {
                auth_username = AS_Main.GetUsername();
                auth_password = AS_Main.GetPassword();
            }
            // Verify 2FA code button
            if (GUILayout.Button(ENTER_2FA_CODE_VERIFY_STRING, GUILayout.Width(ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH)))
            {
                checkingCode = true;
                APIUser.VerifyTwoFactorAuthCode(authenticationCode, isValidAuthenticationCode ? API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION : API2FA.ONE_TIME_PASSWORD_AUTHENTICATION, auth_username, auth_password,
                        delegate
                        {
                        // valid 2FA code submitted
                        entered2faCodeIsInvalid = false;
                            authorizationCodeWasVerified = true;
                            checkingCode = false;
                            showTwoFactorAuthenticationEntry = false;
                            if (null != AS_Main.onAuthenticationVerifiedAction)
                                AS_Main.onAuthenticationVerifiedAction();
                        },
                        delegate
                        {
                            entered2faCodeIsInvalid = true;
                            checkingCode = false;
                        }
                    );
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // after user has entered an invalid code causing the invalid code message to be displayed,
            // edit the code will change it's length meaning it is invalid format, so we can clear the invalid code setting until they resubmit
            if (previousAuthenticationCodeLength != authenticationCode.Length)
            {
                previousAuthenticationCodeLength = authenticationCode.Length;
                entered2faCodeIsInvalid = false;
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // Two-Factor Authentication Help button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(ENTER_2FA_CODE_HELP_STRING))
            {
                Application.OpenURL(TWO_FACTOR_AUTHENTICATION_HELP_URL);
            }
            EditorGUILayout.EndHorizontal();

            // Cancel button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(ENTER_2FA_CODE_CANCEL_STRING))
            {
                showTwoFactorAuthenticationEntry = false;
                AS_Main.Logout(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        static bool IsValidAuthenticationCodeFormat()
        {
            bool isValid2faAuthenticationCode = false;

            if (!string.IsNullOrEmpty(authenticationCode))
            {
                // check if the input is a valid 6-digit numberic code (ignoring spaces)
                Regex rx = new Regex(@"^(\s*\d\s*){6}$", RegexOptions.Compiled);
                MatchCollection matches6DigitCode = rx.Matches(authenticationCode);
                isValid2faAuthenticationCode = (matches6DigitCode.Count == 1);
            }

            return isValid2faAuthenticationCode;
        }
    }
}
#endif