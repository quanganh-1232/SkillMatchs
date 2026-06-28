using System.Text;
using System.Text.Json;

namespace SkillMatch.Views.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _geminiApiKey = configuration["GeminiSettings:ApiKey"]
                ?? throw new ArgumentNullException("Không tìm thấy cấu hình Gemini API Key trong appsettings.json");
        }

        public async Task<string> GetJobSuggestionsAsync(string userQuery, string availableJobsRaw)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
                return "Bạn vui lòng nhập nội dung hoặc vị trí công việc cần tư vấn nhé!";

            if (string.IsNullOrWhiteSpace(availableJobsRaw) || availableJobsRaw.Length < 10)
            {
                availableJobsRaw = "[Lưu ý: Hệ thống hiện tại chưa nạp được danh sách công việc tĩnh từ database, hãy tư vấn chung dựa trên kiến thức của bạn].";
            }

            // CẬP NHẬT PROMPT: Ép AI sinh thẻ HTML <a> để dẫn đến trang Details của Jobs Controller
            string prompt = $"Bạn là một Trợ lý AI định hướng nghề nghiệp thông minh tích hợp trên hệ thống kết nối sinh viên SkillMatch.\n" +
                            $"Nhiệm vụ trọng tâm: Đối chiếu yêu cầu người dùng và đề xuất các dự án phù hợp nhất.\n\n" +
                            $"[YÊU CẦU/KỸ NĂNG CỦA SINH VIÊN]:\n" +
                            $"'{userQuery}'\n\n" +
                            $"[DANH SÁCH DỰ ÁN THỰC TẾ TRONG DATABASE]:\n" +
                            $"{availableJobsRaw}\n\n" +
                            $"[QUY TẮC PHÂN TÍCH VÀ ĐỀ XUẤT - BẮT BUỘC]:\n" +
                            $"1. Hãy tìm kiếm theo NGỮ NGHĨA và TỪ KHÓA LIÊN QUAN thay vì chỉ khớp từ khóa chính xác. " +
                            $"Ví dụ: Nếu sinh viên gõ 'Thiết kế UI/UX cho website công ty xây dựng', hãy đối chiếu ngay với các việc có tiêu đề 'Thiết kế giao diện UI/UX...', hoặc các dự án yêu cầu Figma/Đồ họa, không được bỏ sót.\n" +
                            $"2. Nếu tìm thấy dự án tương thích, hãy liệt kê rõ ràng: Tiêu đề công việc, Mức ngân sách (Budget) và Hạn chót (Deadline).\n" +
                            $"3. ĐẶC BIỆT (QUAN TRỌNG NHẤT): Dưới mỗi dự án được gợi ý, bạn BẮT BUỘC phải chèn một liên kết HTML chuẩn theo cấu trúc sau để người dùng nhấn vào xem chi tiết: <br/><a href='/Jobs/Details/ID_CỦA_DỰ_ÁN' class='btn btn-sm btn-outline-primary rounded-pill mt-2 d-inline-block fw-bold' style='text-decoration:none;'>👉 Xem chi tiết & Ứng tuyển</a> (Thay thế cụm 'ID_CỦA_DỰ_ÁN' bằng số ID thực tế của công việc đó, ví dụ: /Jobs/Details/1).\n" +
                            $"4. Nếu trong database không có dự án nào liên quan trực tiếp, hãy đề xuất 1-2 công việc gần giống nhất đang có kèm lời khuyên, TUYỆT ĐỐI không trả lời cụt ngủn.\n" +
                            $"5. Văn phong: Giọng điệu tự nhiên, lịch sự, chuyên nghiệp, sử dụng thẻ <br/> để xuống dòng thay cho kí tự \\n để giao diện HTML hiển thị đẹp mắt.\n\n" +
                            $"Hãy bắt đầu câu trả lời một cách tự nhiên và đi thẳng vào phân tích nhu cầu.";

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };


            var jsonPayload = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    return "Hiện tại hệ thống AI ChatBot đang bận xử lý, bạn vui lòng thử lại sau vài giây!";
                }
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var textResult = parts[0].GetProperty("text").GetString();
                        return textResult ?? "Xin lỗi, không có dữ liệu văn bản nào được trả về từ máy chủ AI.";
                    }
                }

                if (root.TryGetProperty("promptFeedback", out var feedback))
                {
                    return "⚠️ Yêu cầu tìm kiếm của bạn chứa từ khóa nhạy cảm bị hệ thống AI từ chối xử lý. Vui lòng nhập từ khóa khác phù hợp với công việc!";
                }

                return "Không thể phân tích được phản hồi từ AI, vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                return $"[Lỗi kết nối AI]: {ex.Message}";
            }
        }
    }
}