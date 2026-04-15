using System;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrepIdeaCenter.Models;



namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjM2I5OTU3OS0wNTI2LTQ3ODYtOGY3ZS0wODUwNWFkZjhkMGYiLCJpYXQiOiIwNC8xNS8yMDI2IDE0OjU1OjI5IiwiVXNlcklkIjoiYjM2ZWJlOWUtYTQzZS00ZTMzLTUzOWQtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJ0ZXN0TWFydGluYUBleGFtcGxlLmNvbSIsIlVzZXJOYW1lIjoidGVzdE5hbWVNYXJ0aW5hIiwiZXhwIjoxNzc2Mjg2NTI5LCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.usHqb4F1-gkqbXv7uiIiy_XVr2_kjtOoHSWppfFvSJY";

        private const string LoginEmail = "testMartina@example.com";
        private const string LoginPassword = "test123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            } else
            {
                jwtToken = GetJwtToken (LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient (options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            
            var response = tempClient.Execute (request);

            if (response.StatusCode == HttpStatusCode.OK) 
            { 
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if(string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in response");
                }
                return token;
            } else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute (request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);
            
            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditLastIdea_ShouldEditSuccessfully()
        {
            var editIdea = new IdeaDTO
            {
                Title = "Test Idea_edit",
                Description = "This is a test idea description_edit.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editIdea);

            var response = this.client.Execute(request);
            var editResponseItem = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            Assert.That(editResponseItem.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");

            //текста който се очаква има грешка в кавичките, но логиката е тази
            Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoutRequiredFields_ShouldReturnBadRequest()

        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code is 400 Bad Request.");
        }

        [Order((6))]
        [Test]
        public void EditNoExistingIdea_ShouldReturnBadRequest()
        {
            string nonExistingId = "99999999";
            var editIdea = new IdeaDTO
            {
                Title = "Test Idea_edit",
                Description = "This is a test idea description_edit.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingId);
            request.AddJsonBody(editIdea);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code is 400 Bad Request.");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [Order(7)]
        [Test]
        public void DeletNonExistingIdea_ShouldReturnBadRequest()
        {
            string nonExistingId = "99999999";
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingId);
            var response = this.client.Execute(request);

            //текста който се очаква има грешка в кавичките, но логиката е тази
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code is 400 Bad Request.");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}