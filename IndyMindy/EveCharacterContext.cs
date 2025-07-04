﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyMindy
{
    public class EveCharacterContext
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }

        public long CharacterID { get; set; }
        public string CharacterName { get; set; }
        public string CharacterPortraitUrl => $"https://images.evetech.net/characters/{CharacterID}/portrait";

        public string[] Scopes { get; set; }

        public string CorporationName { get; set; }
        public long CorporationID { get; set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken) && TokenExpiration > DateTime.UtcNow;
    }
}
