# PWNEU Backend Server

The official backend server of PWNEU.

### 📚 Prerequisites

- [Git](https://git-scm.com)
- [.NET 9](https://dotnet.microsoft.com/download)
- [Docker Compose](https://www.docker.com)

### 🚀 Getting Started (For development only)

- Clone the repository.

```sh
git clone https://github.com/pwneu/pwneu.git
cd pwneu
```

- Generate `.env` file the using `.env.example` file.

```sh
cp .env.example .env # On Linux
Copy-Item .env.example .env # On Windows
```

If on Linux, create directories with broad permissions for mounted volumes.

```
mkdir .containers
mkdir .containers/grafana
mkdir .containers/postgresql
mkdir .containers/prometheus
sudo chmod -R 777 .containers
```

- Build and run the container

```sh
docker compose up -d --build
```

- The endpoints can be found at http://localhost:37100/scalar/v1 or http://localhost:37100/swagger/index.html.

### 📜 License

Before using our program, please refer to our [License](https://github.com/pwneu/pwneu/blob/main/LICENSE).

### 📚 External Resources

Resources that helped me to develop the server.

- [Vertical Slice Architecture](https://www.youtube.com/watch?v=msjnfdeDCmo)
- [Docker Compose ASP.NET Core](https://www.youtube.com/watch?v=WQFx2m5Ub9M)
- [Pagination](https://www.youtube.com/watch?v=X8zRvXbirMU)
- [Result Pattern](https://www.youtube.com/watch?v=WCCkEe_Hy2Y)
- [Fusion Cache](https://www.youtube.com/watch?v=wGKSNqxN4KE)
- [Integration Testing](https://www.youtube.com/watch?v=tj5ZCtvgXKY)
- [Prometheus and Grafana](https://www.youtube.com/watch?v=ePYQEl_ZxCs)
- [Rate Limiting via IP Address](https://www.youtube.com/watch?v=PIfGHbvuAtM)
- [SMTP Client for Development](https://www.youtube.com/watch?v=KtCjH-1iCIk)
- [Semantic Kernel](https://www.youtube.com/watch?v=f_hqGlt_2E8)

### 🙏 Thanks!

Thank you for using our program (〃￣ ω ￣〃).
