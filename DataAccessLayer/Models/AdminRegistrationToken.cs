using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models
{
    public class AdminRegistrationToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
