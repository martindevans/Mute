using System;
using System.Collections.Generic;
using System.Text;

namespace Mute.Services
{
    public class DatabaseService
    {
        private readonly DatabaseConfig _config;

        public DatabaseService(DatabaseConfig config)
        {
            _config = config;
        }
    }
}
