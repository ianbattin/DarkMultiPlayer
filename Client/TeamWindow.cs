using DarkMultiPlayerCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DarkMultiPlayer
{
    public class TeamWindow
    {
        public bool display = false;
        public bool inTeam = false;
        private bool safeDisplay = false;
        private bool initialized = false;
        private static TeamWindow singleton;
        //private parts
        private float lastUpdateTime;
        private string newTeamName = "";
        private string password = "";
        //GUI Layout
        private Rect windowRect;
        private Rect moveRect;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] textAreaOptions;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle textAreaStyle;
        //const
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 350;
        private const float DISPLAY_UPDATE_INTERVAL = .2f;

        public static TeamWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width - (WINDOW_WIDTH + 50), (Screen.height / 2f) - (WINDOW_HEIGHT / 2f), WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);

            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);

            textAreaStyle = new GUIStyle(GUI.skin.textField);

            textAreaOptions = new GUILayoutOption[1];
            textAreaOptions[0] = GUILayout.ExpandWidth(true);

            labelStyle = new GUIStyle(GUI.skin.label);
        }

        public void Draw()
        {
            if (safeDisplay) {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                }
                windowRect = DMPGuiUtil.PreventOffscreenWindow(GUILayout.Window(6705 + Client.WINDOW_OFFSET, windowRect, DrawContent, "DarkMultiPlayer - Debug", windowStyle, layoutOptions));
            }
        }

        private void DrawContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
            {
                GUILayout.Label("TeamName:", labelStyle);
                newTeamName = GUILayout.TextField(newTeamName, 32);
                GUILayout.Label("Password:", labelStyle);
                password = GUILayout.TextField(password, 32);
                if (GUILayout.Button("Join Team", buttonStyle))
                {
                    // pressed join team?
                    DarkLog.Debug("Trying to join team: "+newTeamName);
                    TeamWorker.fetch.sendTeamJoinRequest(newTeamName, password);
                }
                if(GUILayout.Button("Create Team", buttonStyle))
                {
                    DarkLog.Debug("Trying to create team: " + newTeamName+ "with password: "+password);
                    TeamWorker.fetch.sendTeamCreateRequest(newTeamName, password);
                }
            } else
            {
                if (GUILayout.Button("Leave Team", buttonStyle))
                {
                    DarkLog.Debug("Leave Team pressed");
                    TeamWorker.fetch.sendTeamLeaveRequest();
                }
            }
            GUILayout.Label("Team count: " + TeamWorker.fetch.teams.Count);
            GUILayout.EndVertical();
        }

        private void DrawTeamList()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Teams",labelStyle);
            foreach(TeamStatus team in TeamWorker.fetch.teams)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(team.teamName);
                if(PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                {
                    if (GUILayout.Button("Join", buttonStyle))
                    {
                        string password = GUILayout.TextField("password");
                        if (GUILayout.Button("Join"))
                        {
                            TeamWorker.fetch.sendTeamJoinRequest(team.teamName, password);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical();
        }

        private void Update()
        {
            safeDisplay = display;
            if (display)
            {
                if (((UnityEngine.Time.realtimeSinceStartup - lastUpdateTime) > DISPLAY_UPDATE_INTERVAL))
                {
                    lastUpdateTime = UnityEngine.Time.realtimeSinceStartup;
                    //update values
                }
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if(singleton != null)
                {
                    singleton.display = false;
                    Client.updateEvent.Remove(singleton.Update);
                    Client.drawEvent.Remove(singleton.Draw);
                }

                singleton = new TeamWindow();
                Client.updateEvent.Add(singleton.Update);
                Client.drawEvent.Add(singleton.Draw);
            }
        }
    }
}
