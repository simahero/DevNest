using SmtpServer;
using SmtpServer.Authentication;

namespace DevNest.Core.Managers.SMTP
{
    public class DummyUserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return new DummyUserAuthenticator();
        }
    }
}
