using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
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
            a_usernames = AddElementToArray(p_usernames, "Add");
            i_add_index = a_usernames.Length - 1;
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
                if (value != i_user_index && value != add_index)
                {
                    EditorPrefs.SetInt("vrc_selected_user", value);
                    i_user_index = value;
                    Logout(false);
                    Login();
                }
                i_user_index = value;
            }
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
                selected_user_index = add_index;
            EditorPrefs.SetString("sdk#username", null);
            EditorPrefs.SetString("sdk#password", null);
            VRC.Tools.ClearCookies();
            APIUser.Logout();
        }

        public static void Remove()
        {
            p_usernames = RemoveElementFromArray(p_usernames, selected_user_index);
            p_passwords = RemoveElementFromArray(p_passwords, selected_user_index);
            InitAUsernames();
            SaveArrayToEditorPrefs<string>("vrc_usernames", p_usernames);
            SaveArrayToEditorPrefs<string>("vrc_passwords", p_passwords);
            Logout(true);
        }

        private static void SaveCredentials(string username, string password)
        {
            p_usernames = AddElementToArray(p_usernames, username);
            p_passwords = AddElementToArray(p_passwords, password);
            InitAUsernames();
            i_user_index = p_usernames.Length - 1;
            EditorPrefs.SetInt("vrc_selected_user", i_user_index);
            SaveArrayToEditorPrefs<string>("vrc_usernames", p_usernames);
            SaveArrayToEditorPrefs<string>("vrc_passwords", p_passwords);
        }

        public static void AddAccount(string username, string password)
        {
            //test credentials
            AttemptLogin(username, password, delegate()
            {
                SaveCredentials(username, password);
            });
        }

        private static void OnAuthenticationCompleted()
        {
            AttemptLogin();
        }

        static string clientInstallPath;
        public static bool signingIn = false;
        static string error = null;


        static bool entered2faCodeIsInvalid;
        static bool authorizationCodeWasVerified = false;

        //static System.Action onAuthenticationVerifiedAction;

        static bool _showTwoFactorAuthenticationEntry = false;

        static bool showTwoFactorAuthenticationEntry
        {
            get
            {
                return _showTwoFactorAuthenticationEntry;
            }
            set
            {
                _showTwoFactorAuthenticationEntry = value;
                if (!_showTwoFactorAuthenticationEntry && !authorizationCodeWasVerified)
                    Logout(false);
            }
        }

        private static void AttemptLogin(string username = null, string password=null, Action successcallback = null)
        {
            signingIn = true;
            if (username == null)
                username = usernames[selected_user_index];
            if (password == null)
                password = p_passwords[selected_user_index];
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

                    AS_Window.RepaintActiveWindow();
                    if (successcallback != null)
                        successcallback.Invoke();
                },
                delegate (ApiModelContainer<APIUser> c)
                {
                    Logout(false);
                    error = c.Error;
                    VRC.Core.Logger.Log("Error logging in: " + error);
                },
                delegate (ApiModelContainer<API2FA> c)
                {
                    if (c.Cookies.ContainsKey("auth"))
                        ApiCredentials.Set(username, username, "vrchat", c.Cookies["auth"]);
                    showTwoFactorAuthenticationEntry = true;
                    //onAuthenticationVerifiedAction = OnAuthenticationCompleted;
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

        public static void RepaintActiveWindow()
        {
            if (window != null)
                window.Repaint();
        }

        private void OnGUI()
        {
            AS_Main.selected_user_index = EditorGUILayout.Popup(AS_Main.selected_user_index, AS_Main.usernames);

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(ACCOUNT_LOGIN_BORDER_SPACING);
            GUILayout.BeginVertical("Account", "window", GUILayout.Height(150), GUILayout.Width(340));

            if (AS_Main.signingIn)
                EditorGUILayout.LabelField("Signing in as " + AS_Main.usernames[AS_Main.selected_user_index] + ".");
            else if (AS_Main.selected_user_index == AS_Main.add_index)
                AddAccountGui();
            else
                AccountGUI();

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
    }
}