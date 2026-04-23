const getUserRoles = () => {
    try {
        return JSON.parse(localStorage.getItem("userRoles")) || {};
    } catch {
        return {};
    }
};

export const isAdmin = () => getUserRoles().isAdmin || false;
export const isEditor = () => getUserRoles().isEditor || false;
export const isReviewer = () => getUserRoles().isReviewer || false;
export const isGuardian = () => getUserRoles().isGuardian || false;
export const isCaptain = () => getUserRoles().isCaptain || false;
