const API_BASE = "http://localhost:5001/api";

function saveToken(token) {
  sessionStorage.setItem("token", token);
}

function getToken() {
  return sessionStorage.getItem("token");
}

function clearToken() {
  sessionStorage.removeItem("token");
}