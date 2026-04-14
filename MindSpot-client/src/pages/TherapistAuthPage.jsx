import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";
import { User, Phone, ArrowLeft, Loader2, Award, Briefcase } from "lucide-react";
import { AuthError } from "@/components/ui/AuthError";

const TherapistAuthPage = () => {
  const [bio, setBio] = useState("");
  const [fullName, setFullName] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [specialties, setSpecialties] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [errors, setErrors] = useState({});
  const navigate = useNavigate(); 
  const [isSuccess, setIsSuccess] = useState(false);

  const validate = () => {
  const newErrors = {};
  
  if (!fullName.trim()) newErrors.fullName = "Full name is required."; 
  if (!bio.trim()) newErrors.bio = "Bio is required."; 
  if (!specialties) newErrors.specialties = "Specialties are required."; 
  
  // ולידציה לרישיון בפורמט 27/4-5 ספרות
  if (!licenseNumber.trim()) {
    newErrors.licenseNumber = "License number is required.";
  } else if (!/^27-\d{4,6}$/.test(licenseNumber)) {
    newErrors.licenseNumber = "Invalid format. Must be 27- followed by 4 or 6 digits.";
  }

  if (!phoneNumber.trim()) {
    newErrors.phoneNumber = "Phone number is required.";
  } else if (!/^\d{3}-?\d{7}$/.test(phoneNumber.replace(/\s/g, ""))) {
    newErrors.phoneNumber = "Please enter a valid phone number.";
  }

  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};

  const handleSubmit = async (e) => {
  e.preventDefault();
  setError("");
  setErrors({}); // ניקוי שגיאות קודמות
  
  if (!validate()) return;
  
  setLoading(true);

  try {
    const response = await fetch("https://localhost:7160/api/Therapists/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ fullName, specialties, bio, licenseNumber, phoneNumber, role: "Therapist" }),
    });

    const data = await response.json();

    if (response.ok) {
      setIsSuccess(true);
      setTimeout(() => { navigate("/"); }, 2000);
    } else {
      if (data.errors) {
        const serverErrors = {};
        // מעבר על כל השגיאות שהשרת החזיר ומיפוי שלהן ל-State שלנו
        if (data.errors.LicenseNumber) serverErrors.licenseNumber = data.errors.LicenseNumber[0];
        if (data.errors.PhoneNumber) serverErrors.phoneNumber = data.errors.PhoneNumber[0];
        if (data.errors.FullName) serverErrors.fullName = data.errors.FullName[0];
        
        setErrors(serverErrors);
      } else {
        setError(data.message || "Registration failed");
      }
    }
  } catch (err) {
    setError("Unable to connect to the server.");
  } finally {
    setLoading(false);
  }
};

  const inputClass = "w-full pl-10 pr-4 py-3 rounded-xl border border-border bg-background text-foreground text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring transition-colors";

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4 py-12">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }} className="w-full max-w-md">
        <Link to="/" className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 transition-colors text-sm"><ArrowLeft size={16} /></Link>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-card">
          <div className="flex items-center justify-center gap-2 mb-2">
            <div className="h-8 w-8 rounded-lg bg-primary/15 flex items-center justify-center"><Briefcase size={16} className="text-primary" /></div>
          </div>
          <div className="text-center mb-6">
            <h1 className="font-display text-2xl font-semibold text-foreground">Mind<span className="text-primary">Spot</span> <span className="text-sm font-body font-medium text-muted-foreground tracking-wide uppercase">Pro</span></h1>
            <p className="text-muted-foreground text-sm mt-2 max-w-xs mx-auto">Join our network of certified professionals</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            <div className="space-y-4">
              <div className="relative">
                <User size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input type="text" placeholder="Full name" value={fullName} onChange={(e) => {setFullName(e.target.value); setErrors(p=>({...p, fullName: ""}))}} className={`${inputClass} ${errors.fullName ? 'border-destructive' : ''}`} />
                <AuthError message={errors.fullName} />
              </div>

              <div className="relative">
                <Briefcase size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input type="text" placeholder="Specialties" value={specialties} onChange={(e) => {setSpecialties(e.target.value); setErrors(p=>({...p, specialties: ""}))}} className={`${inputClass} ${errors.specialties ? 'border-destructive' : ''}`} />
                <AuthError message={errors.specialties} />
              </div>

              <div className="relative">
                <User size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input type="text" placeholder="Bio" value={bio} onChange={(e) => {setBio(e.target.value); setErrors(p=>({...p, bio: ""}))}} className={`${inputClass} ${errors.bio ? 'border-destructive' : ''}`} />
                <AuthError message={errors.bio} />
              </div>

              <div className="relative">
                <Phone size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input type="tel" placeholder="Phone number" value={phoneNumber} onChange={(e) => {setPhoneNumber(e.target.value); setErrors(p=>({...p, phoneNumber: ""}))}} className={`${inputClass} ${errors.phoneNumber ? 'border-destructive' : ''}`} />
                <AuthError message={errors.phoneNumber} />
              </div>

              <div className="relative">
                <Award size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input type="text" placeholder="Professional license number" value={licenseNumber} onChange={(e) => {setLicenseNumber(e.target.value); setErrors(p=>({...p, licenseNumber: ""}))}} className={`${inputClass} ${errors.licenseNumber ? 'border-destructive' : ''}`} />
                <AuthError message={errors.licenseNumber} />
              </div>
            </div>

            {error && <p className="text-destructive text-sm font-medium">{error}</p>}
            <Button type="submit" className="w-full rounded-xl h-12" disabled={loading || isSuccess}>
              {loading ? <Loader2 size={18} className="animate-spin" /> : isSuccess ? "Submitted Successfully! Redirecting..." : "Submit request for joining"}
            </Button>
          </form>
        </div>
      </motion.div>
    </div>
  );
};

export default TherapistAuthPage;