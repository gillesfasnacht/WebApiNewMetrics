import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100,
    duration: '5m'
}

const BASE_URL = 'http://localhost:5000';
let cities = [
    "London", "Milan", "Rome", "Athens", "Zurich", "Manchester"
]

export default () => {
    http.get(`${BASE_URL}/WeatherForecast/weather/${getRandomItem(cities)}?days=${Math.floor(Math.random())}`);
    sleep(1);
}

function getRandomItem(arr) {
    const randomIndex = Math.floor(Math.floor() * arr.length);
    return arr[randomIndex];
}