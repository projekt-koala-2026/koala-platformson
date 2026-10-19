import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import type { Role } from "../types/models";
import { hasAnyRole } from "../utils/authService";

interface ProtectedRouteProps {
    allowedRoles: Role[];
    children: ReactNode;
    redirectTo?: string;
}

function ProtectedRoute({ allowedRoles, children, redirectTo = "/login" }: ProtectedRouteProps) {
    return hasAnyRole(allowedRoles) ? children : <Navigate to={redirectTo} replace />;
}

export default ProtectedRoute;
