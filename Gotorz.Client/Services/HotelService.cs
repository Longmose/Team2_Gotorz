using Gotorz.Shared.DTOs;
using System.Net.Http.Json;

namespace Gotorz.Client.Services
{
    public class HotelService : IHotelService
    {
        private readonly HttpClient _httpClient;

        public HotelService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<HotelDto>> GetHotelsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<HotelDto>>("/api/hotel");
            return response ?? new List<HotelDto>();
        }

        public async Task AddHotelAsync(HotelDto hotel)
        {
            await _httpClient.PostAsJsonAsync("/api/hotel", hotel);
        }

        public async Task<List<HotelDto>> GetHotelsByCityName(string city, string country, DateTime arrival, DateTime departure)
        {
            var query = $"/api/hotel/search?city={city}&country={country}&arrival={arrival:yyyy-MM-dd}&departure={departure:yyyy-MM-dd}";
            var response = await _httpClient.GetFromJsonAsync<List<HotelDto>>(query);
            return response ?? new List<HotelDto>();
        }
       
        public async Task<List<HotelRoomDto>> GetHotelRoomsByHotelId(string externalHotelId, DateTime arrival, DateTime departure)
        {
            var query = $"/api/hotel/rooms?externalHotelId={externalHotelId}&arrival={arrival:yyyy-MM-dd}&departure={departure:yyyy-MM-dd}";
            var response = await _httpClient.GetFromJsonAsync<List<HotelRoomDto>>(query);
            return response ?? new List<HotelRoomDto>();
        }

        public async Task BookHotelAsync(HotelBookingDto booking)
        {
            await _httpClient.PostAsJsonAsync("/api/hotelbooking", booking);
        }
    }
}
