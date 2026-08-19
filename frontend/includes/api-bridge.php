<?php
require_once 'config.php';

// This file acts as a secure proxy for frontend AJAX calls to the C# backend
if (!isset($_SESSION['user'])) {
    http_response_code(401);
    echo json_encode(['error' => 'Unauthorized']);
    exit;
}

$endpoint = $_GET['endpoint'] ?? '';
$method = $_SERVER['REQUEST_METHOD'];
$data = null;

if ($method !== 'GET') {
    $input = file_get_contents('php://input');
    $data = json_decode($input, true);
}

$result = api_call($endpoint, $method, $data, $_SESSION['token'] ?? null);
http_response_code($result['code']);
header('Content-Type: application/json');
echo json_encode($result['data']);
?>
