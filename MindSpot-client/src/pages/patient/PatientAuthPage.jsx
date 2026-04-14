import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";
import { Mail, Lock, User, ArrowLeft, Loader2 } from "lucide-react";
import { AuthError } from "@/components/ui/AuthError";

const PatientAuthPage = () => {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [errors, setErrors] = useState({});
  const navigate = useNavigate();

  const validate = () => {
    const newErrors = {};
    if (!isLogin && !displayName.trim()) newErrors.displayName = "Full name is required.";
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) newErrors.email = "Please enter a valid email address.";
    if (password.length < 6) newErrors.password = "Password must be at least 6 characters.";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setErrors({});
    if (!validate()) return;

    setLoading(true);
    try {
      const endpoint = isLogin ? "https://localhost:7160/api/Auth/login" : "https://localhost:7160/api/Patients/register";
      const requestBody = isLogin 
        ? { Email: email, Password: password, Role: "Patient" }
        : { FullName: displayName, Email: email, Password: password, Role: "Patient" };

      const response = await fetch(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(requestBody),
      });

      if (response.ok) {
        const data = await response.json();
        if (isLogin) {
          sessionStorage.clear(); 
          if (data.token) sessionStorage.setItem("token", data.token);
          if (data.userId) sessionStorage.setItem("userId", data.userId);
          sessionStorage.setItem("role", "patient");
          navigate("/patient-dashboard");
        } else {
          setIsLogin(true); 
          setPassword(""); 
        }
      } else {
        const data = await response.json();
        setError(data.message || "Authentication failed");
      }
    } catch (err) {
      setError("Unable to connect to the server.");
    } finally {
      setLoading(false);
    }
  };
    
  const inputClass = "w-full pl-10 pr-4 py-3 rounded-xl border border-border bg-background text-foreground text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring transition-colors";

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }} className="w-full max-w-md">
        <Link to="/" className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-8 transition-colors"><ArrowLeft size={16} /> Back to home</Link>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-card">
          <div className="text-center mb-6">
            <h1 className="font-display text-2xl font-semibold text-foreground">Mind<span className="text-primary">Spot</span></h1>
            <p className="text-muted-foreground text-sm mt-2">{isLogin ? "Welcome back" : "Create your account"}</p>
          </div>

          <div className="flex bg-muted rounded-xl p-1 mb-6">
            {["Sign In", "Create Account"].map((label, i) => (
                <button key={label} type="button" onClick={() => { setIsLogin(i === 0); setError(""); setErrors({}); }}
                  className={`flex-1 py-2 text-sm font-medium rounded-lg transition-all ${((i === 0 && isLogin) || (i === 1 && !isLogin)) ? "bg-card text-foreground shadow-soft" : "text-muted-foreground hover:text-foreground"}`}>
                  {label}
                </button>
            ))}
          </div>

          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            {!isLogin && (
                <div className="relative">
                  <User size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                  <input type="text" placeholder="Full name" value={displayName} onChange={(e) => {setDisplayName(e.target.value); setErrors(p=>({...p, displayName: ""}))}} className={`${inputClass} ${errors.displayName ? 'border-destructive' : ''}`} />
                  <AuthError message={errors.displayName} />
                </div>
            )}

            <div className="relative">
              <Mail size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
              <input type="email" placeholder="Email address" value={email} onChange={(e) => {setEmail(e.target.value); setErrors(p=>({...p, email: ""}))}} className={`${inputClass} ${errors.email ? 'border-destructive' : ''}`} />
              <AuthError message={errors.email} />
            </div>

            <div className="relative">
              <Lock size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
              <input type="password" placeholder="Password" value={password} onChange={(e) => {setPassword(e.target.value); setErrors(p=>({...p, password: ""}))}} className={`${inputClass} ${errors.password ? 'border-destructive' : ''}`} />
              <AuthError message={errors.password} />
            </div>

            {error && <p className="text-destructive text-sm font-medium">{error}</p>}
            <Button type="submit" className="w-full rounded-xl h-12" disabled={loading}>
              {loading ? <Loader2 size={18} className="animate-spin" /> : isLogin ? "Sign In" : "Create Account"}
            </Button>
          </form>
        </div>
      </motion.div>
    </div>
  );
};

export default PatientAuthPage;