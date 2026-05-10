const API_URL = "http://192.168.100.8:8000";

export async function sendDecision(data) {
  const response = await fetch(`${API_URL}/decision`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  });

  return response.json();
}
