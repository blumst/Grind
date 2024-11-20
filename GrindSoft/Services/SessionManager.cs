using GrindSoft.Models;
using System.Collections.Concurrent;

namespace GrindSoft.Services
{
    public class SessionManager
    {
        private readonly ConcurrentQueue<Session> _sessions = new();

        public void AddSession(Session session)
        {
            _sessions.Enqueue(session);
        }

        public bool TryGetNextSession(out Session session)
        {
            return _sessions.TryDequeue(out session);
        }

        public int GetSessions()
        {
            return _sessions.Count;
        }
    }
}
