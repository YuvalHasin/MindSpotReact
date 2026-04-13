import { Navigate } from "react-router-dom";

const ProtectedRoute = ({ children, redirectTo, roleRequired }) => {
  const token = sessionStorage.getItem("token");
  const userRole = sessionStorage.getItem("role"); // למשל "admin" או "patient"

  if (!token) {
    return <Navigate to={redirectTo} replace />;
  }

  // אם הגדרנו שהנתיב דורש תפקיד מסוים והוא לא מתאים
  if (roleRequired && userRole !== roleRequired) {
    return <Navigate to="/" replace />; // שליחה לדף הבית אם הוא מנסה "לפרוץ"
  }

  return children;
};

export default ProtectedRoute;