using System;
using System.Collections.Generic;
using System.Text;

namespace StravaSub
{
    public  class AuthModel
    {

        public string client_id { get; set; }

        public string client_secret { get; set; }

        public string grant_type { get; set; }

        public string code { get; set; }


    }
}
