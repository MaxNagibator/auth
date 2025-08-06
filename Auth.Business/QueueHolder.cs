using System.Collections.Concurrent;

namespace Auth.Business;

public class QueueHolder
{
    public ConcurrentQueue<MailMessage> MailMessages { get; } = new();
}
