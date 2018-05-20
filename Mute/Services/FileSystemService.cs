using System;

namespace Mute.Services
{
    public class FileSystemService
    {
        private readonly DatabaseService _db;

        public FileSystemService(DatabaseService db)
        {
            _db = db;
        }

        public void AllowDirectoryAccess(string path)
        {
            throw new NotImplementedException();
        }
    }
}
