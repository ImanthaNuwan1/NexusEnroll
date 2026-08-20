<?php
session_start();

// ── Database / API Configuration ──
define('DB_HOST', 'localhost');
define('DB_USER', 'root');
define('DB_PASS', '');
define('DB_NAME', 'nexusenroll_db');

// C# Backend API Base URL (your .NET app should expose REST endpoints)
// Override with the API_BASE_URL environment variable if the backend isn't
// reachable at localhost from wherever PHP is running.
define('API_BASE_URL', getenv('API_BASE_URL') ?: 'http://localhost:5000/api');
define('API_KEY', 'your-api-key-here'); // Optional: if your C# backend requires an API key

// ── App Settings ──
define('APP_NAME', 'NexusEnroll');
define('APP_URL', 'http://localhost/nexusenroll');
define('TIMEZONE', 'Asia/Colombo');
date_default_timezone_set(TIMEZONE);

// ── Error Display (disable in production) ──
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

// ── Helper: API Call to C# Backend ──
function api_call($endpoint, $method = 'GET', $data = null, $token = null) {
    $url = API_BASE_URL . $endpoint;
    $ch = curl_init($url);

    $headers = [
        'Content-Type: application/json',
        'Accept: application/json'
    ];
    if ($token) $headers[] = 'Authorization: Bearer ' . $token;
    if (defined('API_KEY')) $headers[] = 'X-API-Key: ' . API_KEY;

    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

    if ($method === 'POST') {
        curl_setopt($ch, CURLOPT_POST, true);
        if ($data) curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
    } elseif ($method !== 'GET') {
        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);
        if ($data) curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
    }

    $response = curl_exec($ch);
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    return [
        'code' => $http_code,
        'data' => json_decode($response, true)
    ];
}

// ── Helper: Flash Messages ──
function set_flash($type, $message) {
    $_SESSION['flash'] = ['type' => $type, 'message' => $message];
}
function get_flash() {
    if (isset($_SESSION['flash'])) {
        $flash = $_SESSION['flash'];
        unset($_SESSION['flash']);
        return $flash;
    }
    return null;
}

// ── Helper: Auth Check ──
function require_auth() {
    if (!isset($_SESSION['user'])) {
        set_flash('error', 'Please login to continue.');
        header('Location: login.php');
        exit;
    }
}
function require_admin() {
    require_auth();
    if ($_SESSION['user']['role'] !== 'Admin') {
        set_flash('error', 'Access denied.');
        header('Location: dashboard.php');
        exit;
    }
}
?>
