using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyMindy
{
    public class UserContext
    {
        public string Email { get; set; }
        public string AuthToken { get; set; }
        public bool IsSubscribed { get; set; }
        public List<EveCharacterContext> Characters { get; set; } = new();
        public ProductionPlanner Planner { get; set; } = new();
    }

    public static class SessionManager
    {
        public static UserContext CurrentUser { get; set; }
    }
}
