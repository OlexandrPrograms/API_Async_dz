using System.Text.Json;

namespace API_Async_dz
{
    public class ApiResponse
    {
        public UserResult[] Results { get; set; }
    }

    public class UserResult
    {
        public Name Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class Name
    {
        public string Title { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;


            Console.Write("Введіть загальну кількість HTTP-запитів: ");
            if (!int.TryParse(Console.ReadLine(), out int totalRequests) || totalRequests <= 0)
            {
                Console.WriteLine("Будь ласка, введіть коректне додатне число.");
                return;
            }

            int coreCount = Environment.ProcessorCount;
            Console.WriteLine($"\nВиявлено логічних ядер процесора: {coreCount}");

            Task<List<UserResult>>[] tasks = new Task<List<UserResult>>[coreCount];

            int baseCount = totalRequests / coreCount;
            int remainder = totalRequests % coreCount;

            for (int i = 0; i < coreCount; i++)
            {
                int requestsForThisCore = baseCount + (i < remainder ? 1 : 0);

                if (requestsForThisCore == 0)
                {
                    tasks[i] = Task.FromResult(new List<UserResult>());
                    continue;
                }
                
                tasks[i] = Task.Run(async () =>
                {
                    var localUsers = new List<UserResult>();
                    using HttpClient client = new HttpClient();

                    for (int j = 0; j < requestsForThisCore; j++)
                    {
                        try
                        {
                            string url = "https://randomuser.me/api/";
                            HttpResponseMessage response = await client.GetAsync(url);

                            if (response.IsSuccessStatusCode)
                            {
                                string jsonContent = await response.Content.ReadAsStringAsync();

                                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonContent, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                if (apiResponse?.Results != null && apiResponse.Results.Length > 0)
                                {
                                    localUsers.Add(apiResponse.Results[0]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Помилка під час запиту: {ex.Message}");
                        }
                    }

                    return localUsers;
                });
            }

            List<UserResult>[] resultsFromAllTasks = await Task.WhenAll(tasks);

            List<UserResult> finalUserList = new List<UserResult>();
            foreach (var item in resultsFromAllTasks)
            {
                finalUserList.AddRange(item);
            }

            Console.WriteLine($"\n=== УСПІШНО ОТРИМАНО КОРИСТУВАЧІВ: {finalUserList.Count} ===");

            int counter = 1;
            foreach (var user in finalUserList)
            {
                Console.WriteLine($"{counter++}. {user.Name?.First} {user.Name?.Last} | Email: {user.Email}");
            }
        }
    }
}
