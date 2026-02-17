using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class TokenCleanupWorker : BackgroundService
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupWorker> _logger;

        public TokenCleanupWorker(IServiceProvider serviceProvider, ILogger<TokenCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token cleanup worker started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        _logger.LogInformation(" Token Cleanup Worker: Checking for expired tokens...");
                        var deletedCount = await context.RefreshTokens
                            .Where(rt => rt.ExpiryDate < DateTime.UtcNow)
                            .ExecuteDeleteAsync(stoppingToken);
                        if (deletedCount > 0)
                        {
                            _logger.LogInformation(" Token Cleanup Worker: Deleted {Count} expired tokens.", deletedCount);
                        }
                        else
                        {
                            _logger.LogInformation(" Token Cleanup Worker: No expired tokens found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired tokens.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute
            }
        }
    }
}
