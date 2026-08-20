using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests.Rules;

public class ApiContractBreakingChangeRuleTests
{
    private const string ContractPath = "contracts/openapi.json";

    [Fact]
    public void Triggers_when_an_endpoint_is_removed()
    {
        const string oldContract = """
        { "paths": { "/customers/{id}": { "get": { "responses": { "200": {} } } } } }
        """;
        const string newContract = """{ "paths": {} }""";

        var findings = Evaluate(oldContract, newContract);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleId.ApiContractBreakingChange);
        findings[0].Severity.Should().Be(AgentGuard.Core.RiskEngine.Severity.High);
        findings[0].Explanation.Should().Contain("removed");
    }

    [Fact]
    public void Triggers_when_an_http_method_is_removed_from_a_remaining_endpoint()
    {
        const string oldContract = """
        {
          "paths": {
            "/customers/{id}": {
              "get": { "responses": { "200": {} } },
              "delete": { "responses": { "204": {} } }
            }
          }
        }
        """;
        const string newContract = """
        { "paths": { "/customers/{id}": { "get": { "responses": { "200": {} } } } } }
        """;

        var findings = Evaluate(oldContract, newContract);

        findings.Should().ContainSingle();
        findings[0].Explanation.Should().Contain("DELETE");
    }

    [Fact]
    public void Triggers_when_a_response_property_is_removed()
    {
        const string oldContract = """
        {
          "paths": {
            "/customers/{id}": {
              "get": {
                "responses": {
                  "200": {
                    "content": {
                      "application/json": {
                        "schema": { "properties": { "id": {}, "email": {} } }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;
        const string newContract = """
        {
          "paths": {
            "/customers/{id}": {
              "get": {
                "responses": {
                  "200": {
                    "content": {
                      "application/json": {
                        "schema": { "properties": { "id": {} } }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        var findings = Evaluate(oldContract, newContract);

        findings.Should().ContainSingle();
        findings[0].Explanation.Should().Contain("email");
    }

    [Fact]
    public void Triggers_when_an_optional_request_property_becomes_required()
    {
        const string oldContract = """
        {
          "paths": {
            "/customers": {
              "post": {
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": {
                        "properties": { "name": {}, "email": {} },
                        "required": ["name"]
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;
        const string newContract = """
        {
          "paths": {
            "/customers": {
              "post": {
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": {
                        "properties": { "name": {}, "email": {} },
                        "required": ["name", "email"]
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        var findings = Evaluate(oldContract, newContract);

        findings.Should().ContainSingle();
        findings[0].Explanation.Should().Contain("email");
    }

    [Fact]
    public void Does_not_trigger_for_a_new_endpoint_or_a_new_optional_property()
    {
        const string oldContract = """
        { "paths": { "/customers": { "get": { "responses": { "200": {
            "content": { "application/json": { "schema": { "properties": { "id": {} } } } } } } } } } }
        """;
        const string newContract = """
        {
          "paths": {
            "/customers": {
              "get": {
                "responses": {
                  "200": {
                    "content": { "application/json": { "schema": { "properties": { "id": {}, "name": {} } } } }
                  }
                }
              }
            },
            "/orders": { "get": { "responses": { "200": {} } } }
          }
        }
        """;

        var findings = Evaluate(oldContract, newContract);

        findings.Should().BeEmpty();
    }

    private static IReadOnlyList<AgentGuard.Core.Findings.Finding> Evaluate(string oldContract, string newContract)
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified(ContractPath, oldContract, newContract));

        return ApiContractBreakingChangeRule.Evaluate(changeSet);
    }
}
