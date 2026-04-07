export const getRole = () => localStorage.getItem("_k_r_");
export const isLogged = () => localStorage.getItem("_k_l_") === "true";
export const isSpecialUser = () => {
    const role = getRole();
    return ["admin", "editor", "reviewer"].includes(role);
};
