using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

// Ví dụ gửi kèm khi tạo/sửa nghĩa. ExampleId > 0 = cập nhật ví dụ sẵn có; null/0 = thêm mới.
// Không có cờ xóa: bỏ ví dụ (soft-delete) chưa hỗ trợ vì bảng word_examples chưa có cột trạng thái.
public sealed record SenseExampleInput(
    [property: JsonPropertyName("example_id")] uint? ExampleId,
    [property: JsonPropertyName("example_en")] string? ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi);
