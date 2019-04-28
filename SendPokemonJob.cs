using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Quartz;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SendAPokemon
{
    public class SendPokemonJob : Quartz.IJob
    {

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Console.WriteLine($"Job executing at {DateTime.Now.ToString("HH:mm:ss")}");
                Console.WriteLine("-----------");
                // Get jobdatamap from and obtain config values
                JobDataMap jobData = context.JobDetail.JobDataMap;
                string pokemonBaseUrl = jobData.GetString("pokemonBaseUrl");
                string webHookUrl = jobData.GetString("webHookUrl");
                await GetPokemonAndSendWebHook(pokemonBaseUrl, webHookUrl);
                // Success
                return;
            }
            catch(Exception ex)
            {
                // Unsuccessful
                Console.WriteLine($"{ex} executing job: {ex.InnerException}");
                return;
            }
        }

       async private static Task<Pokemon> GetRandomPokemon(HttpClient client, string pokemonBaseUrl)
        {
            int pokemonId = new Random().Next(0, 807); // Generate random pokedex number
            HttpResponseMessage responseMessage = await client.GetAsync(pokemonBaseUrl + pokemonId.ToString()); // await request to API
            responseMessage.EnsureSuccessStatusCode();
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            // Elected to Deserialize to an anonymous type as we only require one value from the API response
            var pokemonObj = new {name = "", sprites = new { front_default = ""}};
            var pokemon = JsonConvert.DeserializeAnonymousType(responseString, pokemonObj);
            return new Pokemon(pokemon.name, pokemon.sprites.front_default); // Get and return the pokemon
        }

        private static StringContent BuildWebhookRequestBody(Pokemon pokemon)
        {
            // Prepare post body
            var requestBody = new 
            { 
                content = $"Your random pokemon is: {pokemon.Name}", 
                embeds = new List<object> 
                { 
                    new { image = new { url = pokemon.Sprite } }
                }
            };
            string requestBodyString = JsonConvert.SerializeObject(requestBody);
            StringContent requestContent = new StringContent(requestBodyString);
            requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            return requestContent;
        }

        async private Task SendWebHook(HttpClient client, string WebHookUrl, Pokemon pokemon)
        {
            StringContent requestContent = BuildWebhookRequestBody(pokemon);
            // Send request and parse response
            HttpResponseMessage responseMessage = await client.PostAsync(WebHookUrl, requestContent);
            responseMessage.EnsureSuccessStatusCode();

        }

        // Here I have wrapped the get pokemon and send webhook functions into another function so they can share the same HTTP client
        async private Task GetPokemonAndSendWebHook(string pokemonBaseUrl, string webHookUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                // Get pokemon name
                Pokemon pokemon = await GetRandomPokemon(client, pokemonBaseUrl);
                Console.WriteLine($"Got pokemon: {pokemon.Name}");
                // Send webook
                Console.WriteLine("Sending webhook...");
                await SendWebHook(client, webHookUrl, pokemon);
                Console.WriteLine("Webhook sent!");
            }
        }
    }
}
