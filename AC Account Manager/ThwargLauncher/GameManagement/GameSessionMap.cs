﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ThwargLauncher
{
    class GameSessionMap
    {
        private static object _locker = new object();
        // Member data
        private Dictionary<int, GameSession> _sessionByProcessId = new Dictionary<int, GameSession>();
        private Dictionary<string, GameSession> _sessionByServerAccount = new Dictionary<string, GameSession>();
        private Dictionary<string, List<GameSession>> _sessionsByCharacterName = new Dictionary<string,List<GameSession>>();

        public void AddGameSession(GameSession gameSession)
        {
            lock (_locker)
            {
                // #1 By ProcessId
                if (_sessionByProcessId.ContainsKey(gameSession.ProcessId))
                {
                    // Note: This can happen at startup
                    Logger.WriteError(string.Format("Duplicate process id in AddGameSession: {0}", gameSession.ProcessId));
                }
                else
                {
                    _sessionByProcessId.Add(gameSession.ProcessId, gameSession);
                }
                // #2 By Server/Account
                string key = GetServerAccountKey(gameSession);
                if (_sessionByServerAccount.ContainsKey(key))
                {
                    Logger.WriteError(string.Format("Duplicate server/account in AddGameSession: {0}", key));
                }
                else
                {
                    _sessionByServerAccount.Add(key, gameSession);
                }
                // #3 By character
                if (gameSession.CharacterName != null)
                {
                    List<GameSession> sessionList = null;
                    if (_sessionsByCharacterName.ContainsKey(gameSession.CharacterName))
                    {
                        sessionList = _sessionsByCharacterName[gameSession.CharacterName];
                    }
                    else
                    {
                        sessionList = new List<GameSession>();
                    }
                    sessionList.Add(gameSession);
                    _sessionsByCharacterName[gameSession.CharacterName] = sessionList;
                }
            }
        }
        public void SetGameSessionProcessId(GameSession gameSession, int processId)
        {
            lock (_locker)
            {
                if (gameSession.ProcessId == processId) { return; }
                // This should only occur with starting game sessions
                // when they find their process id after finding their heartbeat file
                if (gameSession.ProcessId != 0)
                {
                    // This should not occur
                    // ProcessId should only change when a starting game session with process id 0 (unknown)
                    // finds its heartbeat file the first time
                }
                _sessionByProcessId.Remove(gameSession.ProcessId);
                gameSession.ProcessId = processId;
                _sessionByProcessId.Add(gameSession.ProcessId, gameSession);
            }
        }
        public bool HasGameSessionByProcessId(int processId)
        {
            return GetGameSessionByProcessId(processId) != null;
        }
        public GameSession GetGameSessionByProcessId(int processId)
        {
            lock (_locker)
            {
                if (_sessionByProcessId.ContainsKey(processId))
                {
                    return _sessionByProcessId[processId];
                }
                else
                {
                    return null;
                }
            }
        }
        public ServerAccountStatus GetGameSessionStateByServerAccount(string serverName, string accountName)
        {
            var gameSession = GetGameSessionByServerAccount(serverName, accountName);
            if (gameSession == null)
            {
                return ServerAccountStatus.None;
            }
            else
            {
                return gameSession.Status;
            }
        }
        public GameSession GetGameSessionByServerAccount(string serverName, string accountName)
        {
            lock (_locker)
            {
                return GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
            }
        }
        private GameSession GetGameSessionByServerAccountImplUnlocked(string serverName, string accountName)
        {
            string key = GetServerAccountKey(serverName, accountName);
            if (_sessionByServerAccount.ContainsKey(key))
            {
                return _sessionByServerAccount[key];
            }
            else
            {
                return null;
            }
        }
        public List<GameSession> GetGameSessionsByCharacterName(string characterName)
        {
            if (_sessionsByCharacterName.ContainsKey(characterName))
            {
                return _sessionsByCharacterName[characterName];
            }
            else
            {
                return new List<GameSession>();
            }
        }
        public List<GameSession> GetAllGameSessions()
        {
            var allStatuses = new List<GameSession>();
            lock (_locker)
            {
                allStatuses.AddRange(_sessionByProcessId.Values);
            }
            return allStatuses;
        }
        public void RemoveGameSessionByProcessId(int processId)
        {
            lock (_locker)
            {
                if (_sessionByProcessId.ContainsKey(processId))
                {
                    GameSession gameSession = _sessionByProcessId[processId];
                    // #1 By ProcessId
                    _sessionByProcessId.Remove(processId);
                    // #2 By Server/Account
                    _sessionByServerAccount.Remove(GetServerAccountKey(gameSession));
                    // #3 By Character Name
                    if (_sessionsByCharacterName.ContainsKey(gameSession.CharacterName))
                    {
                        _sessionsByCharacterName[gameSession.CharacterName].Remove(gameSession);
                    }
                }
            }
        }
        public void StartLaunchingSession(string serverName, string accountName)
        {
            lock (_locker)
            {
                var gameSession = GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
                if (gameSession != null)
                {
                    gameSession.Status = ServerAccountStatus.Starting;
                }
                else
                {
                    gameSession = new GameSession();
                    gameSession.ServerName = serverName;
                    gameSession.AccountName = accountName;
                    gameSession.Status = ServerAccountStatus.Starting;
                    AddGameSession(gameSession);
                }
            }
        }
        public void EndLaunchingSession(string serverName, string accountName)
        {
            lock (_locker)
            {
                var gameSession = GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
                if (gameSession != null)
                {
                    if (gameSession.Status == ServerAccountStatus.Starting)
                    {
                        // If it never made it out of starting, then it should be set to warning
                        gameSession.Status = ServerAccountStatus.Warning;
                    }
                }
            }
        }
        public void EndAllLaunchingSessions()
        {
            lock (_locker)
            {
                foreach (var gameSession in _sessionByProcessId.Values)
                {
                    if (gameSession.Status == ServerAccountStatus.Starting)
                    {
                        gameSession.Status = ServerAccountStatus.Warning;
                    }
                }
            }
        }
        /// <summary>
        /// Return latest launch times for all accounts
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DateTime> GetLaunchAccountTimes()
        {
            var accountLaunchTimes = new Dictionary<string, DateTime>();
            lock (_locker)
            {
                foreach (var gameSession in _sessionByProcessId.Values)
                {
                    if (gameSession.UptimeSeconds == -1) { continue; }
                    DateTime launch = DateTime.UtcNow - TimeSpan.FromSeconds(gameSession.UptimeSeconds);
                    if (!accountLaunchTimes.ContainsKey(gameSession.AccountName)
                        || launch > accountLaunchTimes[gameSession.AccountName])
                    {
                        accountLaunchTimes[gameSession.AccountName] = launch;
                    }
                }

            }
            return accountLaunchTimes;
        }

        private string GetServerAccountKey(GameSession gameSession)
        {
            return GetServerAccountKey(gameSession.ServerName, gameSession.AccountName);
        }
        private string GetServerAccountKey(string serverName, string accountName)
        {
            string key = string.Format("{0}:{1}", serverName, accountName);
            return key;
        }
    }
}
