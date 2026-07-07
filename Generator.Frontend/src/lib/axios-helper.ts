import axios, { type AxiosInstance, type AxiosRequestConfig } from 'axios';

class AxiosService {
  private _axios: AxiosInstance;

  constructor() {
    this._axios = axios.create({
      baseURL: 'http://localhost:5092/', // https://localhost:7138 // import.meta.env.VITE_API_URL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    });

    // Request Interceptor
    this._axios.interceptors.request.use(
      config => {
        // const token = localStorage.getItem('token');
        // if (token) {
        //   config.headers.Authorization = `Bearer ${token}`;
        // }
        return config;
      },
      error => Promise.reject(error)
    );

    // Response Interceptor
    this._axios.interceptors.response.use(
      response => response,
      error => {
        // if (error.response?.status === 401) {
        // }
        return Promise.reject(error);
      }
    );
  }

  async get<TResponse = undefined>(url: string, config?: AxiosRequestConfig | undefined): Promise<TResponse | undefined> {
    const response = await this._axios.get<TResponse>(url, config);
    return response.data;
  }

  async post<TResponse = undefined>(url: string, data?: unknown, config?: AxiosRequestConfig | undefined): Promise<TResponse | undefined> {
    const response = await this._axios.post<TResponse>(url, data, config);
    return response.data;
  }

  async put<TResponse = undefined>(url: string, data?: unknown, config?: AxiosRequestConfig | undefined): Promise<TResponse | undefined> {
    const response = await this._axios.put<TResponse>(url, data, config);
    return response.data;
  }

  async delete<TResponse = undefined>(url: string, data?: unknown, config?: AxiosRequestConfig | undefined): Promise<TResponse | undefined> {
    const response = await this._axios.delete<TResponse>(url, {
      data: data,
      ...config
    });
    return response.data;
  }
}

export default new AxiosService();
