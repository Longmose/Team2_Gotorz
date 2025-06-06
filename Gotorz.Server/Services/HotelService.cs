using System.Text.Json.Nodes;
using Gotorz.Server.Models;
using Gotorz.Server.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gotorz.Server.Services
{
    public class HotelService : IHotelService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IRepository<Hotel> _hotelRepository;
        private readonly IHotelRoomRepository _hotelRoomRepository;
        private readonly IRepository<HotelBooking> _hotelBookingRepository;

        public HotelService(
            HttpClient httpClient,
            IConfiguration config,
            IRepository<Hotel> hotelRepository,
            IHotelRoomRepository hotelRoomRepository,
            IRepository<HotelBooking> hotelBookingRepository)
        {
            _httpClient = httpClient;
            _config = config;
            _hotelRepository = hotelRepository;
            _hotelRoomRepository = hotelRoomRepository;
            _hotelBookingRepository = hotelBookingRepository;
        }

        // Search and store hotels if not already in DB
        public async Task<List<Hotel>> GetHotelsByCityName(string city, string country, DateTime arrival, DateTime departure)
        {
            var hotels = new List<Hotel>();

            // Get destination coordinates
            var locationRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={city}"),
                Headers =
                {
                    { "x-rapidapi-key", _config["RapidAPI:Hotels:Key"] },
                    { "x-rapidapi-host", _config["RapidAPI:Hotels:Host"] },
                },
            };

            using var locationResponse = await _httpClient.SendAsync(locationRequest);
            var locationBody = await locationResponse.Content.ReadAsStringAsync();
            var locationRoot = JsonNode.Parse(locationBody);

            var bestMatch = locationRoot?["data"]?.AsArray()?.FirstOrDefault(item =>
                item?["dest_type"]?.ToString() == "city" &&
                item?["country"]?.ToString()?.ToLower().Contains(country.ToLower()) == true &&
                item?["name"]?.ToString()?.ToLower().Contains(city.ToLower()) == true);

            if (bestMatch == null) return hotels;

            var lat = bestMatch["latitude"]?.GetValue<double>() ?? 0;
            var lon = bestMatch["longitude"]?.GetValue<double>() ?? 0;
            if (lat == 0 || lon == 0) return hotels;

            // Get hotel list by coordinates
            string arrivalStr = arrival.ToString("yyyy-MM-dd");
            string departureStr = departure.ToString("yyyy-MM-dd");

            var hotelRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotelsByCoordinates?latitude={lat}&longitude={lon}&adults=1&room_qty=1&units=metric&page_number=1&locale=en-us&currency_code=EUR&arrival_date={arrivalStr}&departure_date={departureStr}"),
                Headers =
                {
                    { "x-rapidapi-key", _config["RapidAPI:Hotels:Key"] },
                    { "x-rapidapi-host", _config["RapidAPI:Hotels:Host"] },
                },
            };

            using var hotelResponse = await _httpClient.SendAsync(hotelRequest);
            var hotelBody = await hotelResponse.Content.ReadAsStringAsync();
            var hotelRoot = JsonNode.Parse(hotelBody);

            var hotelArray = hotelRoot?["data"]?["result"]?.AsArray();
            if (hotelArray == null) return hotels;

            var allHotels = await _hotelRepository.GetAllAsync();
            foreach (var h in hotelArray)
            {
                var address = h?["address"]?.ToString() ?? $"{h?["district"]} {h?["zip"]} {h?["city"]}".Trim();
                if (string.IsNullOrWhiteSpace(address)) address = "Unknown";

                hotels.Add(new Hotel
                {
                    Name = h?["hotel_name"]?.ToString() ?? "Unknown",
                    Address = address,
                    Rating = (int)(h?["review_score"]?.GetValue<double>() ?? 0),
                    Latitude = h?["latitude"]?.GetValue<double>() ?? 0,
                    Longitude = h?["longitude"]?.GetValue<double>() ?? 0,
                    ExternalHotelId = h?["hotel_id"]?.ToString() ?? "N/A"
                });
            }

            foreach (var hotel in hotels)
            {
                var matchingHotel = allHotels.FirstOrDefault(a => a.ExternalHotelId == hotel.ExternalHotelId);
                if (matchingHotel == null)
                {
                    await _hotelRepository.AddAsync(hotel);
                }
            }
            return hotels;
        }

        // First check DB for hotel rooms; if not found, call external API
        public async Task<List<HotelRoom>> GetHotelRoomsAsync(string externalHotelId, DateTime arrival, DateTime departure)
        {
            var hotel = (await _hotelRepository.GetAllAsync())
            .FirstOrDefault(h => h.ExternalHotelId == externalHotelId);
            if (hotel == null)
            {
                return null;
            }

            
            var arrivalStr = arrival.ToString("yyyy-MM-dd");
            var departureStr = departure.ToString("yyyy-MM-dd");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/getRoomListWithAvailability?hotel_id={externalHotelId}&arrival_date={arrivalStr}&departure_date={departureStr}&adults=1&room_qty=1&units=metric&temperature_unit=c&currency_code=EUR&languagecode=en-us"),
                Headers =
                {
                    { "x-rapidapi-key", _config["RapidAPI:Hotels:Key"] },
                    { "x-rapidapi-host", _config["RapidAPI:Hotels:Host"] },
                },
            };

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(body);

            var roomArray = json?["available"]?.AsArray();
            if (roomArray == null) return new List<HotelRoom>();

            var rooms = new List<HotelRoom>();

            foreach (var room in roomArray)
            {
                int.TryParse(room?["max_occupancy"]?.ToString(), out var maxGuests);

                rooms.Add(new HotelRoom
                {
                    HotelId = hotel.HotelId,
                    ExternalRoomId = room?["room_id"]?.ToString() ?? "0",
                    Name = room?["name"]?.ToString() ?? "Unknown",
                    Description = room?["description"]?.ToString(),
                    Capacity = maxGuests,
                    Price = room?["product_price_breakdown"]?["gross_amount"]?["value"]?.GetValue<decimal>() ?? 0,
                    MealPlan = room?["mealplan"]?.ToString(),
                    Refundable = room?["policy_display_details"]?["refund_during_fc"]?["title_details"]?["translation"] != null,
                    ArrivalDate = arrival,
                    DepartureDate = departure
                });
            }

            foreach (var r in rooms)
            {
                await _hotelRoomRepository.AddAsync(r);
            }

            return rooms;
        }

        public async Task BookHotelAsync(HotelBooking booking)
        {
            await _hotelBookingRepository.AddAsync(booking);
        }
    }
}