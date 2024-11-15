using System.Text.Json;

namespace HUDUnlimited.Classes;

public static class JsonSettings {
	public static readonly JsonSerializerOptions SerializerOptions = new() {
		IncludeFields = true,
	};
}