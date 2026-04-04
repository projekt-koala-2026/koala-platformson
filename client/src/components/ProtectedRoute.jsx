import { Navigate } from "react-router-dom";

function ProtectedRoute({ children }) {
    const userRole = localStorage.getItem("KOALA_ROLE");

    if (!userRole) {
        return <Navigate to="/login" replace />;
    }

    return children;
}
