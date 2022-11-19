﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMC.Core.Objs.Game;

public record AuthenticateObj
{
    public record Agent
    { 
        public string name { get; set; }
        public string version { get; set; }
    }
    public Agent agent { get; set; }
    public string username { get; set; }
    public string password { get; set; }
    public string clientToken { get; set; }
    public bool requestUser { get; set; }
}

public record AuthenticateResObj
{
    public record SelectedProfile
    {
        public string name { get; set; }
        public string id { get; set; }
    }
    public record User
    { 
        public string id { get; set; }
    }
    public string accessToken { get; set; }
    public string clientToken { get; set; }
    public SelectedProfile selectedProfile { get; set; }
    public List<SelectedProfile> availableProfiles { get; set; }
    public User user { get; set; }
}
