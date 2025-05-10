#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace ASC.Tests
{
    public class FakeSession : ISession
    {
        private Dictionary<string, byte[]> sessionFactory = new Dictionary<string, byte[]>();

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => sessionFactory.Keys;

        public void Clear()
        {
            sessionFactory.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            sessionFactory.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            sessionFactory[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (sessionFactory.TryGetValue(key, out value))
            {
                return true;
            }

            value = null; // Không còn cảnh báo nullable vì đã tắt kiểm tra nullable
            return false;
        }
    }
}
