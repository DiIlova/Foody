using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using FoodyPrep.Models;

namespace FoodyPrep
{
    [TestFixture]
    public class FoodyPrep
    {
        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Dani123", "didi123456");//username, password

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        //Example for status code
        // Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        //Example for Message
        // Assert.That(response.Content, Does.Contain("No food revues..."));
        [Test, Order(1)]
        public void CreateFood_WithRequiredFields_ShouldReturnCreated()
        {
            var request = new RestRequest("/api/Food/Create", Method.Post);

            var food = new FoodDTO
            {
                Name = "TestFood",
                Description = "TestDescription",
                Url = ""
            };
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code to be Created (201).");

            //var json = JObject.Parse(response.Content);
            //Assert.That(json.ContainsKey("foodId"), Is.True, "Expected response body to contain 'foodId' property");

            //createdFoodId = json["foodId"]?.ToString() ?? string.Empty;
            //Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Expected foodId to be present in the response.");      

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(jsonResponse, Has.Property("FoodId"), "Expected response to contain 'FoodId' property.");
            Assert.That(jsonResponse.FoodId, Is.Not.Null.And.Not.Empty, "Expected FoodId to be present in the response.");

            createdFoodId = jsonResponse.FoodId;
        }

        [Test, Order(2)]
        public void EditCreatedFoodTitle_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            var edited = new[]
            {
                     new {
                     path = "/name",
                     op = "replace",
                     value = "string"
                     }
            };

            request.AddJsonBody(edited);
            var response = client.Execute(request);

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");
            Assert.That(jsonResponse.Msg, Is.EqualTo("Successfully edited"), "Expected response message to indicate successful edit.");
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnListOfFoods()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");

            var foods = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);

            Assert.That(foods, Is.Not.Empty, "Expected the list of foods to be not empty.");
            Assert.That(foods.Count, Is.GreaterThan(0), "Expected the list of foods to contain at least one item.");
            Assert.That(foods.Any(), Is.True, "Expected the list of foods to contain items.");
        }

        [Test, Order(4)]
        public void DeleteEditedFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");
            Assert.That(response.Content, Does.Contain("Deleted successfully!"), "Expected response message to indicate successful deletion.");

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(jsonResponse.Msg, Is.EqualTo("Deleted successfully!"), "Expected Msg to indicate successful deletion.");
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Food/Create", Method.Post);

            var food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code to be BadRequest (400).");
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            var nonExistingFoodId = 559999999;
            var request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);

            var edited = new[]
            {
                new {
                    path = "/name",
                    op = "replace",
                    value = "Non Existing Food"
                }
            };

            request.AddJsonBody(edited);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code to be NotFound (404).");
            Assert.That(response.Content, Does.Contain("No food revues..."), "Expected response message to indicate that the food was not found.");
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            var nonExistingFoodId = 559999999;

            var request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code to be BadRequest (400).");
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"), "Expected response message to indicate that the food could not be deleted.");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}