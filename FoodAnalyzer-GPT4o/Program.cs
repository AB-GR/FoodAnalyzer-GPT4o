using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using OpenAI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodAnalyzer_GPT4o
{
    internal class Program
    {
        static List<MealInformation> mealInformationList = new List<MealInformation>
        {
            new MealInformation { MealName = "Pancake", CaloriePerServing = 200 },
            new MealInformation { MealName = "BlackForest", CaloriePerServing = 300 },
            new MealInformation { MealName = "Dosa", CaloriePerServing = 80 }
        };

        static List<MealConsumption> mealConsumptionList = new List<MealConsumption>();


        static void Main(string[] args)
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Welcome to the Food Analyzer Console App!");
                Console.WriteLine("Please select an action:");
                Console.WriteLine("1. Add Meal Information");
                Console.WriteLine("2. Show Meal Information");
                Console.WriteLine("3. Upload Meal Image");
                Console.WriteLine("4. Show Calorie Count for the Day");
                Console.WriteLine("5. Exit");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddMealInformation();
                        break;
                    case "2":
                        ShowMealInformation();
                        break;
                    case "3":
                        UploadMealImage();
                        break;
                    case "4":
                        ShowCalorieCountForTheDay();
                        break;
                    case "5":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }

                if (!exit)
                {
                    Console.WriteLine("\nPress any key to return to the menu...");
                    Console.ReadKey();
                }
            }
        }

        static void AddMealInformation()
        {
            Console.WriteLine("Enter meal information. Type 'end' as the meal name to finish.");

            while (true)
            {
                Console.Write("Enter Meal Name: ");
                string mealName = Console.ReadLine();

                if (mealName.ToLower() == "end")
                {
                    break;
                }

                Console.Write("Enter Calories Per Serving: ");
                if (int.TryParse(Console.ReadLine(), out int caloriePerServing))
                {
                    // Add the new meal information to the list
                    mealInformationList.Add(new MealInformation
                    {
                        MealName = mealName,
                        CaloriePerServing = caloriePerServing
                    });

                    Console.WriteLine("Meal information added successfully!\n");
                }
                else
                {
                    Console.WriteLine("Invalid calorie input. Please enter a number.");
                }
            }
        }

        static void ShowMealInformation()
        {
            Console.Clear();
            Console.WriteLine("List of all meal information:");

            if (mealInformationList.Count == 0)
            {
                Console.WriteLine("No meal information available.");
            }
            else
            {
                foreach (var meal in mealInformationList)
                {
                    Console.WriteLine($"Meal Name: {meal.MealName}, Calories per Serving: {meal.CaloriePerServing}");
                }
            }
        }

        static void UploadMealImage()
        {
            string mealImagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MealImages");

            if (!Directory.Exists(mealImagesDirectory))
            {
                Console.WriteLine("MealImages folder does not exist.");
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Select an image number to process or choose Exit:");

                // List all images in the MealImages directory
                var imageFiles = Directory.GetFiles(mealImagesDirectory, "*.*")
                                          .Where(file => file.EndsWith(".jpg") || file.EndsWith(".png") || file.EndsWith(".jpeg"))
                                          .ToArray();

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(imageFiles[i])}");
                }

                Console.WriteLine($"{imageFiles.Length + 1}. Exit");

                // Get user input
                string input = Console.ReadLine();
                if (int.TryParse(input, out int choice))
                {
                    if (choice == imageFiles.Length + 1)
                    {
                        // Exit option chosen
                        break;
                    }
                    else if (choice > 0 && choice <= imageFiles.Length)
                    {
                        // Valid image selected, process the image (implementation in next step)
                        string selectedImage = imageFiles[choice - 1];
                        ProcessImage(selectedImage);
                        break;
                    }
                }

                // If invalid option is chosen, prompt again
                Console.WriteLine("Invalid choice. Please choose a valid image number.");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static async Task ProcessImage(string imagePath)
        {
            // Initialize the OpenAI API client
            string apiKey = "your-openai-api-key"; // Replace with your actual API key

            IOpenAIService openAIService = new OpenAIService(new OpenAiOptions { ApiKey = apiKey });

            // Get comma-separated list of dishes from mealInformationList
            string dishesList = string.Join(", ", mealInformationList.Select(m => m.MealName));

            // Prepare the prompts
            string systemPrompt = "You are a master chef who helps with identifying different dishes from images";
            string userPrompt = $"Return only Json Content.Identify if the dish or dishes belong to any of these {{ {dishesList} }} & try to arrive at the number of servings of each identified dish. The answer should be in the JSON format: " +
                                "[{\"mealName\": \"Pancake\", \"noOfServings\": 2}, {\"mealName\": \"Ice cream\", \"noOfServings\": 2}]. If no dish matches {dishesList}, return an empty JSON array.";

            // Read the image file
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64String = Convert.ToBase64String(imageBytes);

            var completionRequest = new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(systemPrompt),
                    ChatMessage.FromUser(new List<MessageContent>
                    {
                        new MessageContent
                        {
                            Type = "image_url",
                            ImageUrl = new MessageImageUrl{Url = $"data:image/jpeg;base64,{base64String}"}
                        },
                        new MessageContent
                        {
                            Type = "text",
                            Text = userPrompt
                        }
                    })
                },
                Temperature = 0.7f,
                Model = Models.Gpt_4o
            };

            // Send the request to the GPT-4o model
            var response = await openAIService.ChatCompletion.CreateCompletion(completionRequest);

            if (response.Successful)
            {
                string jsonResponse = response.Choices.First().Message.Content;

                // Parse the JSON response to a C# object
                var mealConsumption = JsonSerializer.Deserialize<List<MealConsumption>>(jsonResponse);

                if (mealConsumption != null && mealConsumption.Count > 0)
                {
                    Console.WriteLine("Identified meals and their servings:");
                    foreach (var meal in mealConsumption)
                    {
                        Console.WriteLine($"Meal: {meal.MealName}, Servings: {meal.NoOfServings}");
                        meal.MealTime = DateTime.Now;
                        mealConsumptionList.Add(meal);
                    }
                }
                else
                {
                    Console.WriteLine("No matching dishes found in the image.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to process the image: {response.Error?.Message}");
            }
        }

        static void ShowCalorieCountForTheDay()
        {
            DateTime today = DateTime.Today;
            var todaysMeals = mealConsumptionList
                .Where(meal => meal.MealTime.Date == today)
                .OrderBy(meal => meal.MealTime)
                .ToList();

            if (todaysMeals.Count == 0)
            {
                Console.WriteLine("No meals consumed today.");
                return;
            }

            int totalCalories = 0;

            Console.WriteLine("Meals consumed today:");

            foreach (var meal in todaysMeals)
            {
                var mealInfo = mealInformationList.FirstOrDefault(m => m.MealName == meal.MealName);
                if (mealInfo != null)
                {
                    int mealCalories = meal.NoOfServings * mealInfo.CaloriePerServing;
                    totalCalories += mealCalories;

                    Console.WriteLine($"{meal.MealTime.ToShortTimeString()} - {meal.MealName} - {meal.NoOfServings} serving(s) - {mealCalories} calories");
                }
                else
                {
                    Console.WriteLine($"{meal.MealTime.ToShortTimeString()} - {meal.MealName} - {meal.NoOfServings} serving(s) - Calorie information not found");
                }
            }

            Console.WriteLine($"\nTotal calorie count for the day: {totalCalories} calories");
        }
    }

    class MealInformation
    {
        public string MealName { get; set; }
        public int CaloriePerServing { get; set; }
    }

    class MealConsumption
    {
        [JsonPropertyName("mealName")]
        public string MealName { get; set; }
        [JsonPropertyName("noOfServings")]
        public int NoOfServings { get; set; }
        public DateTime MealTime { get; set; }
    }

}
