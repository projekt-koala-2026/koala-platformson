import { isSpecialUser, clearAuth } from "./authService";

export const apiRequest = async (url, options, navigate) => {
    try {
        const response = await fetch(url, options);

        if (response.status === 401) {
            const special = isSpecialUser();
            clearAuth();

            if (special) navigate("/admin/login");
            else navigate("/login");

            return null;
        }

        const json = await response.json();

        if (json.meta) {
            localStorage.setItem("_k_r_", json.meta.role);
            localStorage.setItem("_k_l_", String(json.meta.isLogged));
        }

        return json.data;
    } catch (error) {
        console.error("Network / server error: ", error);
        return null;
    }
};
