using Octokit;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Elf.Api.Services;

/// <summary>
/// Service for interacting with GitHub Issues.
/// </summary>
public class GitHubIssuesService
{
    private readonly GitHubClient _client;
    private readonly IConfiguration _configuration;
    private readonly int _appId;
    private readonly long _installationId;
    private readonly string _enterpriseUrl;
    private DateTime _tokenExpiration;
    private readonly string _keyVaultUrl;

    /// <summary>
    /// Constructor for GitHubIssuesService.
    /// </summary>
    /// <param name="configuration"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public GitHubIssuesService(IConfiguration configuration)
    {
        _configuration = configuration;

        _appId = int.Parse(configuration["GitHubEnterprise:AppId"]
            ?? throw new InvalidOperationException("GitHubEnterprise:AppId is not configured."));
        _installationId = long.Parse(configuration["GitHubEnterprise:InstallationId"]
            ?? throw new InvalidOperationException("GitHubEnterprise:InstallationId is not configured."));
        _enterpriseUrl = configuration["GitHubEnterprise:EnterpriseUrl"]
            ?? throw new InvalidOperationException("GitHubEnterprise:EnterpriseUrl is not configured.");
        _keyVaultUrl = configuration["AzureKeyVault:Url"]
            ?? throw new InvalidOperationException("AzureKeyVault:Url is not configured.");
        _client = InitializeGitHubClient();
    }

    /// <summary>
    /// Retrieve all issues for a specific repository, optionally filtered by labels.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="repo"></param>
    /// <param name="labels"></param>
    /// <param name="includeComments"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<Models.Issue>> GetIssuesWithCommentsAsync(string owner, string repo, string[]? labels = null, bool includeComments = false)
    {
        var issues = await GetAllIssuesAsync(owner, repo, labels);

        // Fetch comments in parallel only if requested
        var issueWithCommentsTasks = issues.Select(async issue =>
        {
            var comments = includeComments
                ? await GetIssueCommentsAsync(owner, repo, issue.Number)
                : Enumerable.Empty<IssueComment>();
            return new Elf.Api.Models.Issue
            {
                Number = issue.Number,
                Body = issue.Body,
                Author = issue.User.Login,
                Title = issue.Title,
                State = issue.State.StringValue,
                Labels = [.. issue.Labels.Select(l => l.Name)],
                CreatedAt = issue.CreatedAt.DateTime,
                UpdatedAt = issue.UpdatedAt?.DateTime ?? default,
                HtmlUrl = issue.HtmlUrl,
                Comments = comments.Select(c => new Elf.Api.Models.Comment
                {
                    Author = c.User.Login,
                    Body = c.Body,
                    CreatedAt = c.CreatedAt.DateTime
                }).ToList() // Ensure Comments is a List
                };
        });

        var issueWithComments = await Task.WhenAll(issueWithCommentsTasks);
        return issueWithComments;
    }

    /// <summary>
    /// Retrieve all comments for a specific issue.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="repo"></param>
    /// <param name="issueNumber"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<IssueComment>> GetIssueCommentsAsync(string owner, string repo, int issueNumber)
    {
        RefreshTokenIfNeeded();

        return await _client.Issue.Comment.GetAllForIssue(owner, repo, issueNumber);
    }    

    /// <summary>
    /// Retrieve all labels for a specific repository.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="repo"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<Models.Label>> GetAllLabelsAsync(string owner, string repo)
    {
        RefreshTokenIfNeeded();

        var allLabels = new List<Label>();
        int page = 1;
        const int pageSize = 100;

        while (true)
        {
            var labels = await _client.Issue.Labels.GetAllForRepository(
                owner, repo, new ApiOptions
                {
                    PageSize = pageSize,
                    PageCount = 1,
                    StartPage = page
                });

            if (labels.Count == 0)
                break;

            allLabels.AddRange(labels);
            page++;
        }

        var apiLabels = allLabels.Select(l => new Elf.Api.Models.Label
        {
            Name = l.Name,
            Description = l.Description ?? string.Empty 
        }).ToList();

        return apiLabels;
    }    

    private async Task<IReadOnlyList<Issue>> GetAllIssuesAsync(string owner, string repo, string[]? labels = null)
    {
        RefreshTokenIfNeeded();

        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All
        };

        // Add labels to the request if provided
        if (labels != null && labels.Length > 0)
        {
            foreach (var label in labels)
            {
                request.Labels.Add(label);
            }
        }

        var allIssues = new List<Issue>();
        int page = 1;
        const int pageSize = 100;

        while (true)
        {
            var issues = await _client.Issue.GetAllForRepository(
                owner, repo, request, new ApiOptions { PageCount = 1, PageSize = pageSize, StartPage = page });

            if (issues.Count == 0)
                break;

            allIssues.AddRange(issues);
            page++;
        }

        return allIssues;
    }

    private GitHubClient InitializeGitHubClient()
    {
        var jwtToken = GenerateJwtToken();
        _tokenExpiration = DateTime.UtcNow.AddMinutes(10); // Set expiration time (10 minutes)

        var appClient = new GitHubClient(new ProductHeaderValue("ELF-App"), new Uri(_enterpriseUrl))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        // Create Installation Token
        var installationToken = appClient.GitHubApps.CreateInstallationToken(_installationId).Result;

        return new GitHubClient(new ProductHeaderValue("ELF-App"), new Uri(_enterpriseUrl))
        {
            Credentials = new Credentials(installationToken.Token)
        };
    }

    private string GenerateJwtToken()
    {
        var privateKey = GetPrivateKeyFromKeyVault();

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);
        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };

        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            notBefore: now,
            expires: now + TimeSpan.FromMinutes(10),
            signingCredentials: signingCredentials,
            claims: new[]
            {
                new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
                new Claim("iss", _appId.ToString(), ClaimValueTypes.Integer),
            }
        );
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);     

        return token;   
    }

    private void RefreshTokenIfNeeded()
    {
        if (DateTime.UtcNow >= _tokenExpiration)
        {
            // Reinitialize the GitHub client with a new JWT and installation token
            _client.Credentials = InitializeGitHubClient().Credentials;
        }
    }    

    private string GetPrivateKeyFromKeyVault()
    {
        var client = new SecretClient(new Uri(_keyVaultUrl), new DefaultAzureCredential());
        KeyVaultSecret secret = client.GetSecret("GitHubPrivateKey");                      
        return secret.Value;
    }

}

