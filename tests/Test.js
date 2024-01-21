import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100,
    duration: '5m'
}

const BASE_URL = 'http://localhost:5000';
let cities = [
    "Paris", "Lyon", "Marseille", "Bordeaux", "Toulouse"
]

export default () => {
    http.get(`${BASE_URL}/WeatherForecast/weather/${randomCity(cities)}?days=${Math.floor(Math.random() * 10) + 1}`);
    sleep(1);
}

function randomCity(cities) {
    let random_index = Math.floor(Math.random() * cities.length);
    let random_city = cities[random_index];
    return random_city;
}