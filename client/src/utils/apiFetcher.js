export const apiUrl = import.meta.env.VITE_API_URL;

const authMiddleware = (response, navigate) => {
    if (response.status !== 200) {
        console.log("Error occured!\nerror status: " + response.status);
        console.log(response.body);
        navigate("/admin/login");
        return null;
    }
    return true;
};

export const apiRequest = async (url, options, method, navigate) => {
    try {
        const fetchConfig = {
            method: method,
            headers: {
                "Content-Type": "application/json",
            },
            credentials: "include",
        };

        if (options && method !== "GET" && method !== "HEAD") {
            fetchConfig.body = JSON.stringify(options);
        }

        const response = await fetch(apiUrl + url, fetchConfig);

        if (!authMiddleware(response, navigate)) return null;

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || `Error: ${response.status}`);
        }

        const contentType = response.headers.get("content-type");

        if (contentType && contentType.includes("application/json")) {
            const json = await response.json();
            return json.data || json;
        }

        return await response.text();
    } catch (error) {
        console.error("Network / server error: ", error);
        return null;
    }
};

export const uploadFile = async (file, title, navigate) => {
    const formData = new FormData();
    formData.append("File", file);
    formData.append("Title", title);

    const response = await fetch(apiUrl + "/api/admin/file/public/file", {
        method: "POST",
        body: formData,
        credentials: "include",
    });

    if (!authMiddleware(response, navigate)) return null;

    return response.ok;
};
